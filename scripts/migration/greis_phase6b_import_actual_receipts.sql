/*
DENTALLA MIS — GREIS
PHASE 6B — ACTUAL RECEIPTS AGGREGATE PER CANONICAL PATIENT
Date: 2026-09-26

APPROVED FORMULA: GREIS_ACTUAL_RECEIPTS_V1

The target is NOT a reconstruction of historical cash operations.
It is one source-aware historical aggregate per canonical Patient.

INCLUDED
1) greis_raw.visits_payments.paym_s for pay_type_id IN (1,3,4):
   1 = cash
   3 = legal entity
   4 = terminal
2) legacy fallback for visits that have NO visits_payments rows:
   visits.sum_paid_final where pay_type_id IN (1,3,4)
3) net external patient-account cash/terminal movements:
   patients_money_transfers.ss where transfer_type_id IN (4,8)

EXCLUDED
- pay_type 0 "Списано со счета пациента" (internal account usage)
- certificate usage
- debt write-off
- transfers between patients
- legacy debt balances
- advance balances
- old allocation state
- reconstruction of old CashMovement / Advance / PatientAccountMovement rows

AUDITED CONTROL TOTAL
Detail external visit payments       88,245,768.50
Legacy fallback                       2,567,091.00
External account deposits/refunds     3,067,353.00
-------------------------------------------------
TOTAL                                93,880,212.50

This script is idempotent.
*/

use Dentalla;
set nocount on;
set xact_abort on;

----------------------------------------------------------------------
-- PRE-BATCH DDL
----------------------------------------------------------------------

if not exists (select 1 from sys.schemas where name=N'finance')
    exec(N'create schema finance');

if not exists (select 1 from sys.schemas where name=N'migration')
    exec(N'create schema migration');

if object_id(N'finance.PatientHistoricalReceiptTotals',N'U') is null
begin
    create table finance.PatientHistoricalReceiptTotals
    (
        Id uniqueidentifier not null
            constraint PK_PatientHistoricalReceiptTotals primary key,
        PatientId uniqueidentifier not null,
        SourceSystem nvarchar(40) not null,
        CalculationCode nvarchar(80) not null,
        PeriodStartLocal date null,
        PeriodEndLocal date null,
        Amount decimal(19,4) not null,
        CurrencyCode nvarchar(3) not null,
        ImportedAtUtc datetimeoffset(7) not null,

        constraint FK_PatientHistoricalReceiptTotals_Patients
            foreign key(PatientId) references dbo.Patients(Id)
    );

    create unique index UX_PatientHistoricalReceiptTotals_Source
        on finance.PatientHistoricalReceiptTotals
        (
            PatientId,
            SourceSystem,
            CalculationCode
        );

    create index IX_PatientHistoricalReceiptTotals_Patient
        on finance.PatientHistoricalReceiptTotals(PatientId);
end;

if object_id(N'migration.GreisPatientReceiptAudit',N'U') is null
begin
    create table migration.GreisPatientReceiptAudit
    (
        DentallaPatientId uniqueidentifier not null
            constraint PK_GreisPatientReceiptAudit primary key,

        DetailedVisitReceiptAmount decimal(19,4) not null,
        LegacyFallbackAmount decimal(19,4) not null,
        AccountExternalNetAmount decimal(19,4) not null,
        TotalReceiptAmount decimal(19,4) not null,

        DetailedPaymentRows int not null,
        FallbackVisitRows int not null,
        AccountTransferRows int not null,

        PeriodStartLocal date null,
        PeriodEndLocal date null,

        CalculationCode nvarchar(80) not null,
        ImportedAtUtc datetimeoffset(7) not null,
        LastVerifiedAtUtc datetimeoffset(7) not null,

        constraint FK_GreisPatientReceiptAudit_Patients
            foreign key(DentallaPatientId) references dbo.Patients(Id)
    );
end;

GO

use Dentalla;
set nocount on;
set xact_abort on;

----------------------------------------------------------------------
-- 0. HARD PRECHECKS
----------------------------------------------------------------------

if object_id(N'greis_raw.visits',N'U') is null
    throw 56200, 'greis_raw.visits missing.',1;

if object_id(N'greis_raw.visits_payments',N'U') is null
    throw 56201, 'greis_raw.visits_payments missing.',1;

if object_id(N'greis_raw.patients_money_transfers',N'U') is null
    throw 56202, 'greis_raw.patients_money_transfers missing.',1;

if object_id(N'integration.ExternalIdentifiers',N'U') is null
    throw 56203, 'integration.ExternalIdentifiers missing.',1;

if (select count(*) from greis_raw.visits_payments)<>12652
    throw 56204, 'Expected exactly 12,652 GREIS visits_payments rows.',1;

if (select count(*) from greis_raw.patients_money_transfers)<>4254
    throw 56205, 'Expected exactly 4,254 GREIS patients_money_transfers rows.',1;

if exists
(
    select 1
    from greis_raw.visits_payments p
    left join greis_raw.visits v
      on v.visit_id=p.visit_id
    left join integration.ExternalIdentifiers x
      on upper(x.SystemCode)=N'GREIS'
     and upper(x.EntityType)=N'PATIENT'
     and x.ExternalId=convert(nvarchar(160),v.patient_id)
    where v.visit_id is null
       or x.InternalEntityId is null
)
    throw 56206, 'A GREIS visits_payments row has no canonical Patient.',1;

if exists
(
    select 1
    from greis_raw.patients_money_transfers t
    left join integration.ExternalIdentifiers x
      on upper(x.SystemCode)=N'GREIS'
     and upper(x.EntityType)=N'PATIENT'
     and x.ExternalId=convert(nvarchar(160),t.patient_id)
    where x.InternalEntityId is null
)
    throw 56207, 'A GREIS patients_money_transfers row has no canonical Patient.',1;

----------------------------------------------------------------------
-- 1. RE-PROVE DETAILED PAYMENT RECONCILIATION
----------------------------------------------------------------------

drop table if exists #pay_by_visit;

select
    p.visit_id,
    sum(convert(decimal(19,4),p.paym_s)) as DetailTotal,
    sum(case when p.pay_type_id=0 then convert(decimal(19,4),p.paym_s) else 0 end) as DetailType0,
    sum(case when p.pay_type_id=1 then convert(decimal(19,4),p.paym_s) else 0 end) as DetailType1,
    sum(case when p.pay_type_id=2 then convert(decimal(19,4),p.paym_s) else 0 end) as DetailType2,
    sum(case when p.pay_type_id=3 then convert(decimal(19,4),p.paym_s) else 0 end) as DetailType3,
    sum(case when p.pay_type_id=4 then convert(decimal(19,4),p.paym_s) else 0 end) as DetailType4,
    count(*) as DetailRows
into #pay_by_visit
from greis_raw.visits_payments p
group by p.visit_id;

if exists
(
    select 1
    from #pay_by_visit d
    join greis_raw.visits v on v.visit_id=d.visit_id
    where abs(coalesce(convert(decimal(19,4),v.sum_paid),0)-d.DetailTotal)>=0.0001
       or abs(coalesce(convert(decimal(19,4),v.sum_paid_type0),0)-d.DetailType0)>=0.0001
       or abs(coalesce(convert(decimal(19,4),v.sum_paid_type1),0)-d.DetailType1)>=0.0001
       or abs(coalesce(convert(decimal(19,4),v.sum_paid_type2),0)-d.DetailType2)>=0.0001
       or abs(coalesce(convert(decimal(19,4),v.sum_paid_type3),0)-d.DetailType3)>=0.0001
       or abs(coalesce(convert(decimal(19,4),v.sum_paid_type4),0)-d.DetailType4)>=0.0001
)
    throw 56208, 'Detailed GREIS payments no longer reconcile to visits aggregate fields.',1;

----------------------------------------------------------------------
-- 2. BUILD SOURCE COMPONENTS
----------------------------------------------------------------------

drop table if exists #detail;
drop table if exists #fallback;
drop table if exists #account_external;

select
    x.InternalEntityId as DentallaPatientId,
    convert(decimal(19,4),p.paym_s) as Amount,
    convert(date,p.paym_d) as EventDate
into #detail
from greis_raw.visits_payments p
join greis_raw.visits v
  on v.visit_id=p.visit_id
join integration.ExternalIdentifiers x
  on upper(x.SystemCode)=N'GREIS'
 and upper(x.EntityType)=N'PATIENT'
 and x.ExternalId=convert(nvarchar(160),v.patient_id)
where p.pay_type_id in (1,3,4);

select
    x.InternalEntityId as DentallaPatientId,
    convert(decimal(19,4),v.sum_paid_final) as Amount,
    convert(date,v.dd) as EventDate
into #fallback
from greis_raw.visits v
join integration.ExternalIdentifiers x
  on upper(x.SystemCode)=N'GREIS'
 and upper(x.EntityType)=N'PATIENT'
 and x.ExternalId=convert(nvarchar(160),v.patient_id)
where not exists
(
    select 1
    from #pay_by_visit d
    where d.visit_id=v.visit_id
)
  and v.pay_type_id in (1,3,4)
  and coalesce(v.sum_paid_final,0)<>0;

select
    x.InternalEntityId as DentallaPatientId,
    convert(decimal(19,4),t.ss) as Amount,
    convert(date,t.dd) as EventDate
into #account_external
from greis_raw.patients_money_transfers t
join integration.ExternalIdentifiers x
  on upper(x.SystemCode)=N'GREIS'
 and upper(x.EntityType)=N'PATIENT'
 and x.ExternalId=convert(nvarchar(160),t.patient_id)
where t.transfer_type_id in (4,8);

----------------------------------------------------------------------
-- 3. CONTROL TOTALS
----------------------------------------------------------------------

declare @detail_total decimal(19,4)=
(
    select coalesce(sum(Amount),0) from #detail
);

declare @fallback_total decimal(19,4)=
(
    select coalesce(sum(Amount),0) from #fallback
);

declare @account_total decimal(19,4)=
(
    select coalesce(sum(Amount),0) from #account_external
);

declare @grand_total decimal(19,4)=
    @detail_total+@fallback_total+@account_total;

if @detail_total<>convert(decimal(19,4),88245768.5000)
    throw 56209, 'Detailed external visit receipts differ from audited 88,245,768.50.',1;

if @fallback_total<>convert(decimal(19,4),2567091.0000)
    throw 56210, 'Legacy fallback differs from audited 2,567,091.00.',1;

if @account_total<>convert(decimal(19,4),3067353.0000)
    throw 56211, 'External account deposits/refunds differ from audited 3,067,353.00.',1;

if @grand_total<>convert(decimal(19,4),93880212.5000)
    throw 56212, 'GREIS actual receipts total differs from audited 93,880,212.50.',1;

----------------------------------------------------------------------
-- 4. BUILD ONE CANONICAL-PATIENT AGGREGATE
----------------------------------------------------------------------

drop table if exists #all_components;
drop table if exists #patient_source;

select
    DentallaPatientId,
    Amount,
    EventDate,
    cast(N'DETAIL' as nvarchar(30)) as Component
into #all_components
from #detail;

insert into #all_components(DentallaPatientId,Amount,EventDate,Component)
select DentallaPatientId,Amount,EventDate,N'LEGACY_FALLBACK'
from #fallback;

insert into #all_components(DentallaPatientId,Amount,EventDate,Component)
select DentallaPatientId,Amount,EventDate,N'ACCOUNT_EXTERNAL'
from #account_external;

select
    DentallaPatientId,
    sum(case when Component=N'DETAIL' then Amount else 0 end) as DetailedVisitReceiptAmount,
    sum(case when Component=N'LEGACY_FALLBACK' then Amount else 0 end) as LegacyFallbackAmount,
    sum(case when Component=N'ACCOUNT_EXTERNAL' then Amount else 0 end) as AccountExternalNetAmount,
    sum(Amount) as TotalReceiptAmount,

    sum(case when Component=N'DETAIL' then 1 else 0 end) as DetailedPaymentRows,
    sum(case when Component=N'LEGACY_FALLBACK' then 1 else 0 end) as FallbackVisitRows,
    sum(case when Component=N'ACCOUNT_EXTERNAL' then 1 else 0 end) as AccountTransferRows,

    min(EventDate) as PeriodStartLocal,
    max(EventDate) as PeriodEndLocal
into #patient_source
from #all_components
group by DentallaPatientId;

if exists
(
    select 1
    from #patient_source
    where TotalReceiptAmount<0
)
    throw 56213, 'At least one canonical Patient has a negative GREIS actual-receipts total.',1;

if
(
    select sum(TotalReceiptAmount)
    from #patient_source
)<>convert(decimal(19,4),93880212.5000)
    throw 56214, 'Per-patient aggregate does not reconcile to 93,880,212.50.',1;

----------------------------------------------------------------------
-- 5. TRANSACTIONAL IMPORT
----------------------------------------------------------------------

declare @before_rows int=
(
    select count(*)
    from finance.PatientHistoricalReceiptTotals
    where SourceSystem=N'GREIS'
      and CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1'
);

begin try
    begin transaction;

    merge finance.PatientHistoricalReceiptTotals as target
    using #patient_source as source
      on target.PatientId=source.DentallaPatientId
     and target.SourceSystem=N'GREIS'
     and target.CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1'
    when matched then
        update set
            PeriodStartLocal=source.PeriodStartLocal,
            PeriodEndLocal=source.PeriodEndLocal,
            Amount=source.TotalReceiptAmount,
            CurrencyCode=N'RUB',
            ImportedAtUtc=sysdatetimeoffset()
    when not matched then
        insert
        (
            Id,
            PatientId,
            SourceSystem,
            CalculationCode,
            PeriodStartLocal,
            PeriodEndLocal,
            Amount,
            CurrencyCode,
            ImportedAtUtc
        )
        values
        (
            newid(),
            source.DentallaPatientId,
            N'GREIS',
            N'GREIS_ACTUAL_RECEIPTS_V1',
            source.PeriodStartLocal,
            source.PeriodEndLocal,
            source.TotalReceiptAmount,
            N'RUB',
            sysdatetimeoffset()
        );

    merge migration.GreisPatientReceiptAudit as target
    using #patient_source as source
      on target.DentallaPatientId=source.DentallaPatientId
    when matched then
        update set
            DetailedVisitReceiptAmount=source.DetailedVisitReceiptAmount,
            LegacyFallbackAmount=source.LegacyFallbackAmount,
            AccountExternalNetAmount=source.AccountExternalNetAmount,
            TotalReceiptAmount=source.TotalReceiptAmount,
            DetailedPaymentRows=source.DetailedPaymentRows,
            FallbackVisitRows=source.FallbackVisitRows,
            AccountTransferRows=source.AccountTransferRows,
            PeriodStartLocal=source.PeriodStartLocal,
            PeriodEndLocal=source.PeriodEndLocal,
            CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1',
            LastVerifiedAtUtc=sysdatetimeoffset()
    when not matched then
        insert
        (
            DentallaPatientId,
            DetailedVisitReceiptAmount,
            LegacyFallbackAmount,
            AccountExternalNetAmount,
            TotalReceiptAmount,
            DetailedPaymentRows,
            FallbackVisitRows,
            AccountTransferRows,
            PeriodStartLocal,
            PeriodEndLocal,
            CalculationCode,
            ImportedAtUtc,
            LastVerifiedAtUtc
        )
        values
        (
            source.DentallaPatientId,
            source.DetailedVisitReceiptAmount,
            source.LegacyFallbackAmount,
            source.AccountExternalNetAmount,
            source.TotalReceiptAmount,
            source.DetailedPaymentRows,
            source.FallbackVisitRows,
            source.AccountTransferRows,
            source.PeriodStartLocal,
            source.PeriodEndLocal,
            N'GREIS_ACTUAL_RECEIPTS_V1',
            sysdatetimeoffset(),
            sysdatetimeoffset()
        );

    if
    (
        select count(*)
        from finance.PatientHistoricalReceiptTotals
        where SourceSystem=N'GREIS'
          and CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1'
    )<>(select count(*) from #patient_source)
        throw 56215, 'Target GREIS receipt-summary row count does not equal source aggregate row count.',1;

    if
    (
        select sum(Amount)
        from finance.PatientHistoricalReceiptTotals
        where SourceSystem=N'GREIS'
          and CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1'
    )<>convert(decimal(19,4),93880212.5000)
        throw 56216, 'Target GREIS receipt-summary total is not 93,880,212.50.',1;

    if
    (
        select sum(TotalReceiptAmount)
        from migration.GreisPatientReceiptAudit
        where CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1'
    )<>convert(decimal(19,4),93880212.5000)
        throw 56217, 'Migration audit total is not 93,880,212.50.',1;

    commit transaction;
end try
begin catch
    if @@trancount>0 rollback transaction;
    throw;
end catch;

----------------------------------------------------------------------
-- 6. RESULTS
----------------------------------------------------------------------

declare @after_rows int=
(
    select count(*)
    from finance.PatientHistoricalReceiptTotals
    where SourceSystem=N'GREIS'
      and CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1'
);

select
    @before_rows as RowsBefore,
    @after_rows as RowsAfter,
    @after_rows-@before_rows as RowsCreatedThisRun,
    @detail_total as DetailedVisitReceipts,
    @fallback_total as LegacyFallback,
    @account_total as AccountExternalNet,
    @grand_total as GrandTotal;

select
    count(*) as CanonicalPatientsWithGreisReceiptActivity,
    sum(case when Amount>0 then 1 else 0 end) as PositiveTotals,
    sum(case when Amount=0 then 1 else 0 end) as ZeroTotals,
    sum(case when Amount<0 then 1 else 0 end) as NegativeTotals,
    sum(Amount) as TotalAmount,
    min(Amount) as MinPatientAmount,
    max(Amount) as MaxPatientAmount,
    min(PeriodStartLocal) as FirstReceiptDate,
    max(PeriodEndLocal) as LastReceiptDate
from finance.PatientHistoricalReceiptTotals
where SourceSystem=N'GREIS'
  and CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1';

select
    count(*) as AuditRows,
    sum(DetailedPaymentRows) as DetailedPaymentRows,
    sum(FallbackVisitRows) as FallbackVisitRows,
    sum(AccountTransferRows) as AccountTransferRows,
    sum(DetailedVisitReceiptAmount) as DetailedVisitReceiptAmount,
    sum(LegacyFallbackAmount) as LegacyFallbackAmount,
    sum(AccountExternalNetAmount) as AccountExternalNetAmount,
    sum(TotalReceiptAmount) as TotalReceiptAmount
from migration.GreisPatientReceiptAudit
where CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1';

select top (100)
    a.DentallaPatientId,
    p.CardNumber,
    p.FullName,
    a.DetailedVisitReceiptAmount,
    a.LegacyFallbackAmount,
    a.AccountExternalNetAmount,
    a.TotalReceiptAmount,
    a.PeriodStartLocal,
    a.PeriodEndLocal
from migration.GreisPatientReceiptAudit a
join dbo.Patients p
  on p.Id=a.DentallaPatientId
where a.CalculationCode=N'GREIS_ACTUAL_RECEIPTS_V1'
order by a.TotalReceiptAmount desc,a.DentallaPatientId;

print '=== GREIS PHASE 6B COMPLETE ===';
