/*
DENTALLA MIS — GREIS
PHASE 5A — TREATMENT PLAN / VTREATMENTS AUDIT
READ ONLY — NO PERSISTENT DATA CHANGED
Date: 2026-09-26

Confirmed before this audit:
- final normalized GREIS clinical appointments = 18,823
- 15,836 Fulfilled appointments have Encounters
- 35 final GREIS visits contain treatment_id
- these 35 visits reference 25 distinct treatment_id values
- all 25 had a current greis_raw.vtreatments row in Phase 4A2
- greis_raw.vtreatments total rows = 27
- greis_raw.history_vtreatments total rows = 1,437
- greis_raw.vtreatments_prescriptions total rows = 5

Purpose:
Determine whether vtreatments are actual patient treatment plans, what fields
they contain, whether one treatment belongs to one patient, how history relates
to current rows, and what must become normalized treatment-plan data versus
immutable legacy provenance.

No target treatment-plan tables are created in this phase.
*/

use Dentalla;
set nocount on;
set xact_abort on;

if object_id(N'greis_raw.vtreatments', N'U') is null
    throw 55000, 'greis_raw.vtreatments missing.', 1;
if object_id(N'greis_raw.history_vtreatments', N'U') is null
    throw 55001, 'greis_raw.history_vtreatments missing.', 1;
if object_id(N'greis_raw.vtreatments_prescriptions', N'U') is null
    throw 55002, 'greis_raw.vtreatments_prescriptions missing.', 1;
if object_id(N'greis_raw.visits', N'U') is null
    throw 55003, 'greis_raw.visits missing.', 1;
if object_id(N'migration.GreisVisitImportMap', N'U') is null
    throw 55004, 'migration.GreisVisitImportMap missing.', 1;
if (select count_big(*) from migration.GreisVisitImportMap) <> 18823
    throw 55005, 'Expected 18,823 final GREIS clinical visits.', 1;

print '=== 1A. vtreatments COLUMNS ===';
select c.column_id,c.name as ColumnName,ty.name as SqlType,c.max_length,c.precision,c.scale,c.is_nullable
from sys.columns c join sys.types ty on ty.user_type_id=c.user_type_id
where c.object_id=object_id(N'greis_raw.vtreatments') order by c.column_id;

print '=== 1B. history_vtreatments COLUMNS ===';
select c.column_id,c.name as ColumnName,ty.name as SqlType,c.max_length,c.precision,c.scale,c.is_nullable
from sys.columns c join sys.types ty on ty.user_type_id=c.user_type_id
where c.object_id=object_id(N'greis_raw.history_vtreatments') order by c.column_id;

print '=== 1C. vtreatments_prescriptions COLUMNS ===';
select c.column_id,c.name as ColumnName,ty.name as SqlType,c.max_length,c.precision,c.scale,c.is_nullable
from sys.columns c join sys.types ty on ty.user_type_id=c.user_type_id
where c.object_id=object_id(N'greis_raw.vtreatments_prescriptions') order by c.column_id;

print '=== 2. ALL CURRENT vtreatments ROWS ===';
select * from greis_raw.vtreatments order by treatment_id;

print '=== 3A. FINAL VISITS WITH treatment_id ===';
select
    m.GreisVisitId,m.MappedStatusCode,m.SourceDate,m.GreisPatientId,m.DentallaPatientId,
    m.GreisDoctorId,m.DentallaStaffProfileId,v.treatment_id,vt.treatment_name,
    vt.treatment_completed,v.comments as VisitComment
from migration.GreisVisitImportMap m
join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
left join greis_raw.vtreatments vt on vt.treatment_id=v.treatment_id
where v.treatment_id is not null
order by v.treatment_id,m.SourceDate,m.GreisVisitId;

print '=== 3B. TREATMENT OWNERSHIP THROUGH VISITS ===';
select
    v.treatment_id,count(*) as LinkedVisits,count(distinct m.GreisPatientId) as DistinctGreisPatients,
    count(distinct m.DentallaPatientId) as DistinctCanonicalPatients,
    count(distinct m.GreisDoctorId) as DistinctPrimaryDoctors,
    min(m.SourceDate) as FirstLinkedVisit,max(m.SourceDate) as LastLinkedVisit,
    max(vt.treatment_name) as CurrentTreatmentName,
    max(convert(int,vt.treatment_completed)) as CurrentTreatmentCompleted
from migration.GreisVisitImportMap m
join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
left join greis_raw.vtreatments vt on vt.treatment_id=v.treatment_id
where v.treatment_id is not null
group by v.treatment_id
order by LinkedVisits desc,v.treatment_id;

print '=== 3C. TREATMENTS LINKED TO >1 CANONICAL PATIENT — MUST BE ZERO OR EXPLAINED ===';
select
    v.treatment_id,count(distinct m.DentallaPatientId) as CanonicalPatients,
    string_agg(convert(nvarchar(max),convert(nvarchar(36),m.DentallaPatientId)),N'; ') as PatientIds
from migration.GreisVisitImportMap m
join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
where v.treatment_id is not null
group by v.treatment_id
having count(distinct m.DentallaPatientId)>1
order by v.treatment_id;

print '=== 4A. VTREATMENTS COVERAGE ===';
select
    (select count(*) from greis_raw.vtreatments) as CurrentVtreatments,
    (select count(distinct v.treatment_id) from migration.GreisVisitImportMap m join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId where v.treatment_id is not null) as TreatmentIdsUsedByFinalVisits,
    (select count(*) from greis_raw.vtreatments vt where exists (select 1 from migration.GreisVisitImportMap m join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId where v.treatment_id=vt.treatment_id)) as CurrentRowsUsedByFinalVisits,
    (select count(*) from greis_raw.vtreatments vt where not exists (select 1 from migration.GreisVisitImportMap m join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId where v.treatment_id=vt.treatment_id)) as CurrentRowsNotReferencedByFinalVisits;

print '=== 4B. UNREFERENCED CURRENT VTREATMENTS ===';
select *
from greis_raw.vtreatments vt
where not exists
(
    select 1 from migration.GreisVisitImportMap m
    join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
    where v.treatment_id=vt.treatment_id
)
order by vt.treatment_id;

print '=== 5A. HISTORY ROW COUNTS BY treatment_id ===';
if col_length(N'greis_raw.history_vtreatments',N'treatment_id') is not null
begin
    exec(N'
        select h.treatment_id,count(*) as HistoryRows,
               max(case when vt.treatment_id is not null then 1 else 0 end) as HasCurrentVtreatment,
               max(vt.treatment_name) as CurrentTreatmentName
        from greis_raw.history_vtreatments h
        left join greis_raw.vtreatments vt on vt.treatment_id=h.treatment_id
        group by h.treatment_id
        order by HistoryRows desc,h.treatment_id;');
end
else
    select N'history_vtreatments has no treatment_id column' as AuditWarning;

print '=== 5B. SAMPLE HISTORY ROWS FOR THE 25 USED TREATMENTS ===';
if col_length(N'greis_raw.history_vtreatments',N'treatment_id') is not null
begin
    exec(N'
        select top (250) h.*
        from greis_raw.history_vtreatments h
        where h.treatment_id in
        (
            select distinct v.treatment_id
            from migration.GreisVisitImportMap m
            join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
            where v.treatment_id is not null
        )
        order by h.treatment_id;');
end;

print '=== 6. ALL vtreatments_prescriptions ROWS ===';
select * from greis_raw.vtreatments_prescriptions;

print '=== 7A. CURRENT DENTALLA TREATMENT/PLAN TABLES ===';
select schema_name(t.schema_id) as SchemaName,t.name as TableName,
       sum(case when p.index_id in (0,1) then p.rows else 0 end) as ApproxRows
from sys.tables t
left join sys.partitions p on p.object_id=t.object_id
where lower(t.name) like N'%treatment%' or lower(t.name) like N'%plan%' or lower(t.name) like N'%prescription%'
group by schema_name(t.schema_id),t.name
order by SchemaName,TableName;

print '=== 7B. CURRENT DENTALLA TREATMENT/PLAN COLUMNS ===';
select schema_name(t.schema_id) as SchemaName,t.name as TableName,c.column_id,c.name as ColumnName,
       ty.name as SqlType,c.max_length,c.precision,c.scale,c.is_nullable
from sys.tables t
join sys.columns c on c.object_id=t.object_id
join sys.types ty on ty.user_type_id=c.user_type_id
where lower(t.name) like N'%treatment%' or lower(t.name) like N'%plan%' or lower(t.name) like N'%prescription%'
order by SchemaName,TableName,c.column_id;

print '=== 8. PHASE 5A SUMMARY ===';
select
    (select count(*) from greis_raw.vtreatments) as CurrentVtreatments,
    (select count(*) from greis_raw.history_vtreatments) as TreatmentHistoryRows,
    (select count(*) from greis_raw.vtreatments_prescriptions) as PrescriptionRows,
    (select count(*) from migration.GreisVisitImportMap m join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId where v.treatment_id is not null) as FinalVisitsWithTreatmentId,
    (select count(distinct v.treatment_id) from migration.GreisVisitImportMap m join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId where v.treatment_id is not null) as DistinctTreatmentIdsUsed,
    (select count(distinct m.DentallaPatientId) from migration.GreisVisitImportMap m join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId where v.treatment_id is not null) as CanonicalPatientsWithTreatmentLinks;

print '=== PHASE 5A COMPLETE: READ ONLY ===';
