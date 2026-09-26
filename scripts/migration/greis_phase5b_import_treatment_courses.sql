/*
DENTALLA MIS — GREIS
PHASE 5B — LEGACY TREATMENT COURSES + PLANNED SERVICES + COURSE HISTORY
Date: 2026-09-26

IMPORTANT SEMANTIC DECISION FROM PHASE 5A
GREIS vtreatments are NOT imported as the new MIS "complex treatment plan".
The source audit calls these entities "курс" (course) and primarily uses them
to group visits:
  - history operation types include "Создание курса", "Добавление визита",
    "Удаление визита", "Измение курса";
  - 27 current vtreatments exist;
  - 35 final clinical visits reference 25 of those course IDs;
  - all 25 referenced courses belong to exactly one canonical patient;
  - 2 current courses are not referenced by final visits (IDs 106 and 132);
  - course 132 contains 5 prescription/planned-service rows.

Therefore target semantics:
  clinical.TreatmentCourses                  -- historical treatment course / episode
  clinical.TreatmentCourseEncounters         -- links course to normalized Encounters
  clinical.TreatmentCoursePlannedServices    -- legacy planned/prescribed service rows
  integration.LegacyTreatmentCourseEvents    -- immutable audit/provenance for all
                                               1,437 history_vtreatments rows

No editable modern comprehensive TreatmentPlan is synthesized from GREIS.

Source counts expected:
  vtreatments                 = 27
  history_vtreatments         = 1,437
  vtreatments_prescriptions   = 5
  final visits with treatment_id = 35
  distinct referenced treatment IDs = 25

This migration is idempotent and source-aware.
*/

use Dentalla;
set nocount on;
set xact_abort on;

----------------------------------------------------------------------
-- PRE-BATCH DDL
----------------------------------------------------------------------

if not exists (select 1 from sys.schemas where name=N'clinical')
    exec(N'create schema clinical');

if not exists (select 1 from sys.schemas where name=N'integration')
    exec(N'create schema integration');

if not exists (select 1 from sys.schemas where name=N'migration')
    exec(N'create schema migration');

if object_id(N'clinical.TreatmentCourses', N'U') is null
begin
    create table clinical.TreatmentCourses
    (
        Id uniqueidentifier not null
            constraint PK_TreatmentCourses primary key,
        PatientId uniqueidentifier not null,
        OwnerStaffProfileId uniqueidentifier null,
        Name nvarchar(500) not null,
        IsCompleted bit not null,
        StartLocal datetime2(7) null,
        EndLocal datetime2(7) null,
        CreatedLocal datetime2(7) null,
        ChangedLocal datetime2(7) null,
        ImportedAtUtc datetimeoffset(7) not null,

        constraint FK_TreatmentCourses_Patients
            foreign key(PatientId) references dbo.Patients(Id),

        constraint FK_TreatmentCourses_StaffProfiles
            foreign key(OwnerStaffProfileId) references staff.StaffProfiles(Id)
    );

    create index IX_TreatmentCourses_PatientId
        on clinical.TreatmentCourses(PatientId);

    create index IX_TreatmentCourses_OwnerStaffProfileId
        on clinical.TreatmentCourses(OwnerStaffProfileId);

    create index IX_TreatmentCourses_PatientId_StartLocal
        on clinical.TreatmentCourses(PatientId,StartLocal);
end;

if object_id(N'clinical.TreatmentCourseEncounters', N'U') is null
begin
    create table clinical.TreatmentCourseEncounters
    (
        TreatmentCourseId uniqueidentifier not null,
        EncounterId uniqueidentifier not null,
        ImportedAtUtc datetimeoffset(7) not null,

        constraint PK_TreatmentCourseEncounters
            primary key(TreatmentCourseId,EncounterId),

        constraint FK_TreatmentCourseEncounters_Courses
            foreign key(TreatmentCourseId)
            references clinical.TreatmentCourses(Id),

        constraint FK_TreatmentCourseEncounters_Encounters
            foreign key(EncounterId)
            references clinical.Encounters(Id)
    );

    create unique index UX_TreatmentCourseEncounters_EncounterId
        on clinical.TreatmentCourseEncounters(EncounterId);
end;

if object_id(N'clinical.TreatmentCoursePlannedServices', N'U') is null
begin
    create table clinical.TreatmentCoursePlannedServices
    (
        Id uniqueidentifier not null
            constraint PK_TreatmentCoursePlannedServices primary key,
        TreatmentCourseId uniqueidentifier not null,
        ServiceCatalogItemId uniqueidentifier not null,
        Quantity decimal(18,4) not null,
        DiscountPercent decimal(9,4) null,
        LegacyToothCode tinyint not null,
        DiagnosisCode nvarchar(50) null,
        CreatedLocal datetime2(7) null,
        ChangedLocal datetime2(7) null,
        ImportedAtUtc datetimeoffset(7) not null,

        constraint FK_TreatmentCoursePlannedServices_Courses
            foreign key(TreatmentCourseId)
            references clinical.TreatmentCourses(Id),

        constraint FK_TreatmentCoursePlannedServices_ServiceCatalogItems
            foreign key(ServiceCatalogItemId)
            references services.ServiceCatalogItems(Id)
    );

    create index IX_TreatmentCoursePlannedServices_Course
        on clinical.TreatmentCoursePlannedServices(TreatmentCourseId);

    create index IX_TreatmentCoursePlannedServices_Service
        on clinical.TreatmentCoursePlannedServices(ServiceCatalogItemId);
end;

if object_id(N'integration.LegacyTreatmentCourseEvents', N'U') is null
begin
    create table integration.LegacyTreatmentCourseEvents
    (
        Id uniqueidentifier not null
            constraint PK_LegacyTreatmentCourseEvents primary key,
        SystemCode nvarchar(40) not null,
        ExternalId nvarchar(160) not null,
        LegacyTreatmentId int not null,
        TreatmentCourseId uniqueidentifier null,
        PatientId uniqueidentifier null,
        StaffProfileId uniqueidentifier null,
        EventLocal datetime2(7) not null,
        TreatmentName nvarchar(500) not null,
        OperationType nvarchar(100) not null,
        ChangeDescription nvarchar(4000) not null,
        DoctorDisplayName nvarchar(300) null,
        PatientDisplayName nvarchar(300) null,
        ImportedAtUtc datetimeoffset(7) not null,

        constraint FK_LegacyTreatmentCourseEvents_Course
            foreign key(TreatmentCourseId)
            references clinical.TreatmentCourses(Id),

        constraint FK_LegacyTreatmentCourseEvents_Patient
            foreign key(PatientId)
            references dbo.Patients(Id),

        constraint FK_LegacyTreatmentCourseEvents_Staff
            foreign key(StaffProfileId)
            references staff.StaffProfiles(Id)
    );

    create unique index UX_LegacyTreatmentCourseEvents_Source
        on integration.LegacyTreatmentCourseEvents(SystemCode,ExternalId);

    create index IX_LegacyTreatmentCourseEvents_LegacyTreatmentId
        on integration.LegacyTreatmentCourseEvents(LegacyTreatmentId,EventLocal);

    create index IX_LegacyTreatmentCourseEvents_PatientId
        on integration.LegacyTreatmentCourseEvents(PatientId,EventLocal);
end;

if object_id(N'migration.GreisTreatmentCourseImportMap', N'U') is null
begin
    create table migration.GreisTreatmentCourseImportMap
    (
        GreisTreatmentId int not null
            constraint PK_GreisTreatmentCourseImportMap primary key,
        DentallaTreatmentCourseId uniqueidentifier not null,
        DentallaPatientId uniqueidentifier not null,
        ImportedAtUtc datetimeoffset(7) not null,
        LastVerifiedAtUtc datetimeoffset(7) not null
    );

    create unique index UX_GreisTreatmentCourseImportMap_Course
        on migration.GreisTreatmentCourseImportMap(DentallaTreatmentCourseId);
end;

if object_id(N'migration.GreisTreatmentCoursePlannedServiceImportMap', N'U') is null
begin
    create table migration.GreisTreatmentCoursePlannedServiceImportMap
    (
        GreisPrescriptionId int not null
            constraint PK_GreisTreatmentCoursePlannedServiceImportMap primary key,
        GreisTreatmentId int not null,
        GreisPriceArticleId int not null,
        DentallaPlannedServiceId uniqueidentifier not null,
        DentallaTreatmentCourseId uniqueidentifier not null,
        DentallaServiceCatalogItemId uniqueidentifier not null,
        ImportedAtUtc datetimeoffset(7) not null,
        LastVerifiedAtUtc datetimeoffset(7) not null
    );

    create unique index UX_GreisTreatmentCoursePlannedServiceImportMap_Target
        on migration.GreisTreatmentCoursePlannedServiceImportMap(DentallaPlannedServiceId);
end;

if object_id(N'migration.GreisTreatmentCourseEventImportMap', N'U') is null
begin
    create table migration.GreisTreatmentCourseEventImportMap
    (
        GreisHistoryId int not null
            constraint PK_GreisTreatmentCourseEventImportMap primary key,
        DentallaLegacyEventId uniqueidentifier not null,
        ImportedAtUtc datetimeoffset(7) not null,
        LastVerifiedAtUtc datetimeoffset(7) not null
    );

    create unique index UX_GreisTreatmentCourseEventImportMap_Target
        on migration.GreisTreatmentCourseEventImportMap(DentallaLegacyEventId);
end;

GO

use Dentalla;
set nocount on;
set xact_abort on;

----------------------------------------------------------------------
-- 0. HARD PRECHECKS
----------------------------------------------------------------------

if object_id(N'greis_raw.vtreatments',N'U') is null
    throw 55200, 'greis_raw.vtreatments missing.', 1;

if object_id(N'greis_raw.history_vtreatments',N'U') is null
    throw 55201, 'greis_raw.history_vtreatments missing.', 1;

if object_id(N'greis_raw.vtreatments_prescriptions',N'U') is null
    throw 55202, 'greis_raw.vtreatments_prescriptions missing.', 1;

if object_id(N'greis_raw.visits',N'U') is null
    throw 55203, 'greis_raw.visits missing.', 1;

if object_id(N'greis_raw.prices_articles',N'U') is null
    throw 55204, 'greis_raw.prices_articles missing.', 1;

if object_id(N'integration.ExternalIdentifiers',N'U') is null
    throw 55205, 'integration.ExternalIdentifiers missing.', 1;

if object_id(N'migration.GreisVisitImportMap',N'U') is null
    throw 55206, 'migration.GreisVisitImportMap missing.', 1;

if object_id(N'migration.GreisEncounterImportMap',N'U') is null
    throw 55207, 'migration.GreisEncounterImportMap missing. Run successful Phase 4B first.', 1;

if (select count(*) from greis_raw.vtreatments) <> 27
    throw 55208, 'Expected exactly 27 current GREIS vtreatments.', 1;

if (select count(*) from greis_raw.history_vtreatments) <> 1437
    throw 55209, 'Expected exactly 1,437 GREIS treatment history rows.', 1;

if (select count(*) from greis_raw.vtreatments_prescriptions) <> 5
    throw 55210, 'Expected exactly 5 GREIS treatment prescription rows.', 1;

if
(
    select count(*)
    from migration.GreisVisitImportMap m
    join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
    where v.treatment_id is not null
) <> 35
    throw 55211, 'Expected exactly 35 final visits with treatment_id.', 1;

if
(
    select count(distinct v.treatment_id)
    from migration.GreisVisitImportMap m
    join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
    where v.treatment_id is not null
) <> 25
    throw 55212, 'Expected exactly 25 distinct treatment IDs referenced by final visits.', 1;

if exists
(
    select v.treatment_id
    from migration.GreisVisitImportMap m
    join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
    where v.treatment_id is not null
    group by v.treatment_id
    having count(distinct m.DentallaPatientId)>1
)
    throw 55213, 'A GREIS treatment course is linked to more than one canonical Patient.', 1;

if exists
(
    select vt.treatment_id
    from greis_raw.vtreatments vt
    left join integration.ExternalIdentifiers x
      on upper(x.SystemCode)=N'GREIS'
     and upper(x.EntityType)=N'PATIENT'
     and x.ExternalId=convert(nvarchar(160),vt.patient_id)
    group by vt.treatment_id
    having count(x.Id)<>1
)
    throw 55214, 'At least one current GREIS course does not resolve to exactly one Patient.', 1;

if exists
(
    select vt.treatment_id
    from greis_raw.vtreatments vt
    left join integration.ExternalIdentifiers x
      on upper(x.SystemCode)=N'GREIS'
     and upper(x.EntityType)=N'STAFFPROFILE'
     and x.ExternalId=convert(nvarchar(160),vt.doctor_id)
    where vt.doctor_id is not null
    group by vt.treatment_id
    having count(x.Id)<>1
)
    throw 55215, 'At least one current GREIS course owner doctor is unresolved.', 1;

if
(
    select count(*)
    from migration.GreisVisitImportMap m
    join greis_raw.visits v on convert(bigint,v.visit_id)=m.GreisVisitId
    join migration.GreisEncounterImportMap e on e.GreisVisitId=m.GreisVisitId
    where v.treatment_id is not null
) <> 35
    throw 55216, 'Not all 35 treatment-linked final visits have normalized Encounters.', 1;

if exists
(
    select prescription_id
    from greis_raw.vtreatments_prescriptions
    group by prescription_id
    having count(*)>1
)
    throw 55217, 'Duplicate GREIS vtreatments_prescriptions.prescription_id found.', 1;

if exists
(
    select 1
    from greis_raw.vtreatments_prescriptions p
    left join greis_raw.prices_articles pa
      on pa.price_article_id=p.price_article_id
    where pa.price_article_id is null
)
    throw 55218, 'A GREIS treatment prescription references a missing price article.', 1;

----------------------------------------------------------------------
-- 1. BUILD CURRENT COURSE SOURCE
----------------------------------------------------------------------

drop table if exists #course_source;

select
    vt.treatment_id as GreisTreatmentId,
    coalesce(existing.InternalEntityId,newid()) as DentallaTreatmentCourseId,
    px.InternalEntityId as DentallaPatientId,
    sx.InternalEntityId as OwnerStaffProfileId,
    left(coalesce(nullif(ltrim(rtrim(convert(nvarchar(500),vt.treatment_name))),N''),
                  concat(N'GREIS course #',vt.treatment_id)),500) as Name,
    convert(bit,vt.treatment_completed) as IsCompleted,
    case
      when vt.d1 is null or convert(date,vt.d1)=convert(date,'19000101',112) then null
      else convert(datetime2(7),vt.d1)
    end as StartLocal,
    case
      when vt.d2 is null or convert(date,vt.d2)=convert(date,'19000101',112) then null
      else convert(datetime2(7),vt.d2)
    end as EndLocal,
    convert(datetime2(7),vt.rec_created_time) as CreatedLocal,
    convert(datetime2(7),vt.last_change_time) as ChangedLocal,
    existing.Id as ExistingExternalIdentifierId
into #course_source
from greis_raw.vtreatments vt
join integration.ExternalIdentifiers px
  on upper(px.SystemCode)=N'GREIS'
 and upper(px.EntityType)=N'PATIENT'
 and px.ExternalId=convert(nvarchar(160),vt.patient_id)
left join integration.ExternalIdentifiers sx
  on upper(sx.SystemCode)=N'GREIS'
 and upper(sx.EntityType)=N'STAFFPROFILE'
 and sx.ExternalId=convert(nvarchar(160),vt.doctor_id)
left join integration.ExternalIdentifiers existing
  on upper(existing.SystemCode)=N'GREIS'
 and upper(existing.EntityType)=N'TREATMENTCOURSE'
 and existing.ExternalId=convert(nvarchar(160),vt.treatment_id);

if (select count(*) from #course_source)<>27
    throw 55219, 'Course source count is not 27.', 1;

----------------------------------------------------------------------
-- 2. ENSURE SERVICE CATALOG ITEMS NEEDED ONLY BY COURSE PRESCRIPTIONS
----------------------------------------------------------------------

drop table if exists #prescription_articles;

select distinct
    p.price_article_id as GreisPriceArticleId,
    coalesce(existing.InternalEntityId,newid()) as DentallaServiceCatalogItemId,
    left(coalesce(nullif(ltrim(rtrim(convert(nvarchar(500),pa.article_name))),N''),
                  concat(N'GREIS service article #',p.price_article_id)),500) as Name,
    nullif(left(ltrim(rtrim(convert(nvarchar(100),pa.article_code))),100),N'') as Code,
    nullif(left(ltrim(rtrim(convert(nvarchar(300),pg.price_group_name))),300),N'') as GroupName,
    existing.Id as ExistingExternalIdentifierId
into #prescription_articles
from greis_raw.vtreatments_prescriptions p
join greis_raw.prices_articles pa
  on pa.price_article_id=p.price_article_id
left join greis_raw.prices_groups pg
  on pg.price_group_id=pa.price_group_id
left join integration.ExternalIdentifiers existing
  on upper(existing.SystemCode)=N'GREIS'
 and upper(existing.EntityType)=N'SERVICECATALOGITEM'
 and existing.ExternalId=convert(nvarchar(160),p.price_article_id);

if (select count(*) from #prescription_articles)<>5
    throw 55220, 'Expected 5 distinct prescribed GREIS price articles.', 1;

----------------------------------------------------------------------
-- 3. BUILD PLANNED-SERVICE SOURCE
----------------------------------------------------------------------

drop table if exists #planned_source;

select
    p.prescription_id as GreisPrescriptionId,
    p.treatment_id as GreisTreatmentId,
    p.price_article_id as GreisPriceArticleId,
    coalesce(existing.InternalEntityId,newid()) as DentallaPlannedServiceId,
    c.DentallaTreatmentCourseId,
    a.DentallaServiceCatalogItemId,
    convert(decimal(18,4),p.q) as Quantity,
    case when p.discount is null then null else convert(decimal(9,4),p.discount) end as DiscountPercent,
    convert(tinyint,p.tooth) as LegacyToothCode,
    nullif(left(ltrim(rtrim(convert(nvarchar(50),p.mkb))),50),N'') as DiagnosisCode,
    convert(datetime2(7),p.rec_created_time) as CreatedLocal,
    convert(datetime2(7),p.last_change_time) as ChangedLocal,
    existing.Id as ExistingExternalIdentifierId
into #planned_source
from greis_raw.vtreatments_prescriptions p
join #course_source c
  on c.GreisTreatmentId=p.treatment_id
join #prescription_articles a
  on a.GreisPriceArticleId=p.price_article_id
left join integration.ExternalIdentifiers existing
  on upper(existing.SystemCode)=N'GREIS'
 and upper(existing.EntityType)=N'TREATMENTCOURSEPLANNEDSERVICE'
 and existing.ExternalId=convert(nvarchar(160),p.prescription_id);

if (select count(*) from #planned_source)<>5
    throw 55221, 'Planned-service source count is not 5.', 1;

----------------------------------------------------------------------
-- 4. BUILD 35 COURSE <-> ENCOUNTER LINKS
----------------------------------------------------------------------

drop table if exists #course_encounter_source;

select
    c.DentallaTreatmentCourseId,
    e.DentallaEncounterId,
    m.GreisVisitId,
    v.treatment_id as GreisTreatmentId
into #course_encounter_source
from migration.GreisVisitImportMap m
join greis_raw.visits v
  on convert(bigint,v.visit_id)=m.GreisVisitId
join #course_source c
  on c.GreisTreatmentId=v.treatment_id
join migration.GreisEncounterImportMap e
  on e.GreisVisitId=m.GreisVisitId
where v.treatment_id is not null;

if (select count(*) from #course_encounter_source)<>35
    throw 55222, 'Course/Encounter link source count is not 35.', 1;

if exists
(
    select DentallaEncounterId
    from #course_encounter_source
    group by DentallaEncounterId
    having count(*)>1
)
    throw 55223, 'One Encounter belongs to multiple GREIS treatment courses.', 1;

----------------------------------------------------------------------
-- 5. BUILD ALL 1,437 LEGACY COURSE HISTORY EVENTS
----------------------------------------------------------------------

drop table if exists #event_source;

select
    h.history_id as GreisHistoryId,
    coalesce(existing.InternalEntityId,newid()) as DentallaLegacyEventId,
    h.treatment_id as LegacyTreatmentId,
    c.DentallaTreatmentCourseId,
    px.InternalEntityId as DentallaPatientId,
    sx.InternalEntityId as DentallaStaffProfileId,
    convert(datetime2(7),h.rec_created_time) as EventLocal,
    left(convert(nvarchar(500),h.treatment_name),500) as TreatmentName,
    left(convert(nvarchar(100),h.operation_type),100) as OperationType,
    left(convert(nvarchar(4000),h.change_description),4000) as ChangeDescription,
    nullif(left(ltrim(rtrim(convert(nvarchar(300),h.dfio))),300),N'') as DoctorDisplayName,
    nullif(left(ltrim(rtrim(convert(nvarchar(300),h.pfio))),300),N'') as PatientDisplayName,
    existing.Id as ExistingExternalIdentifierId
into #event_source
from greis_raw.history_vtreatments h
left join #course_source c
  on c.GreisTreatmentId=h.treatment_id
left join integration.ExternalIdentifiers px
  on upper(px.SystemCode)=N'GREIS'
 and upper(px.EntityType)=N'PATIENT'
 and px.ExternalId=convert(nvarchar(160),h.patient_id)
left join integration.ExternalIdentifiers sx
  on upper(sx.SystemCode)=N'GREIS'
 and upper(sx.EntityType)=N'STAFFPROFILE'
 and sx.ExternalId=convert(nvarchar(160),h.doctor_id)
left join integration.ExternalIdentifiers existing
  on upper(existing.SystemCode)=N'GREIS'
 and upper(existing.EntityType)=N'LEGACYTREATMENTCOURSEEVENT'
 and existing.ExternalId=convert(nvarchar(160),h.history_id);

if (select count(*) from #event_source)<>1437
    throw 55224, 'Legacy treatment-course event source count is not 1,437.', 1;

----------------------------------------------------------------------
-- 6. TRANSACTIONAL IMPORT
----------------------------------------------------------------------

declare @course_ext_before bigint =
(
    select count_big(*)
    from integration.ExternalIdentifiers
    where upper(SystemCode)=N'GREIS'
      and upper(EntityType)=N'TREATMENTCOURSE'
);

declare @planned_ext_before bigint =
(
    select count_big(*)
    from integration.ExternalIdentifiers
    where upper(SystemCode)=N'GREIS'
      and upper(EntityType)=N'TREATMENTCOURSEPLANNEDSERVICE'
);

declare @event_ext_before bigint =
(
    select count_big(*)
    from integration.ExternalIdentifiers
    where upper(SystemCode)=N'GREIS'
      and upper(EntityType)=N'LEGACYTREATMENTCOURSEEVENT'
);

declare @service_ext_before bigint =
(
    select count_big(*)
    from integration.ExternalIdentifiers
    where upper(SystemCode)=N'GREIS'
      and upper(EntityType)=N'SERVICECATALOGITEM'
);

begin try
    begin transaction;

    insert into services.ServiceCatalogItems
    (
        Id,Name,Code,GroupName,IsHistorical,ImportedAtUtc
    )
    select
        a.DentallaServiceCatalogItemId,
        a.Name,
        a.Code,
        a.GroupName,
        cast(1 as bit),
        sysdatetimeoffset()
    from #prescription_articles a
    where a.ExistingExternalIdentifierId is null;

    insert into integration.ExternalIdentifiers
    (
        Id,SystemCode,EntityType,ExternalId,InternalEntityId,ImportedAtUtc
    )
    select
        newid(),
        N'GREIS',
        N'ServiceCatalogItem',
        convert(nvarchar(160),a.GreisPriceArticleId),
        a.DentallaServiceCatalogItemId,
        sysdatetimeoffset()
    from #prescription_articles a
    where a.ExistingExternalIdentifierId is null;

    if object_id(N'migration.GreisServiceCatalogImportMap',N'U') is not null
    begin
        merge migration.GreisServiceCatalogImportMap as target
        using #prescription_articles as source
        on target.GreisPriceArticleId=source.GreisPriceArticleId
        when matched then
            update set
                DentallaServiceCatalogItemId=source.DentallaServiceCatalogItemId,
                LastVerifiedAtUtc=sysdatetimeoffset()
        when not matched then
            insert
            (
                GreisPriceArticleId,
                DentallaServiceCatalogItemId,
                ImportedAtUtc,
                LastVerifiedAtUtc
            )
            values
            (
                source.GreisPriceArticleId,
                source.DentallaServiceCatalogItemId,
                sysdatetimeoffset(),
                sysdatetimeoffset()
            );
    end;

    insert into clinical.TreatmentCourses
    (
        Id,PatientId,OwnerStaffProfileId,Name,IsCompleted,
        StartLocal,EndLocal,CreatedLocal,ChangedLocal,ImportedAtUtc
    )
    select
        c.DentallaTreatmentCourseId,
        c.DentallaPatientId,
        c.OwnerStaffProfileId,
        c.Name,
        c.IsCompleted,
        c.StartLocal,
        c.EndLocal,
        c.CreatedLocal,
        c.ChangedLocal,
        sysdatetimeoffset()
    from #course_source c
    where c.ExistingExternalIdentifierId is null;

    insert into integration.ExternalIdentifiers
    (
        Id,SystemCode,EntityType,ExternalId,InternalEntityId,ImportedAtUtc
    )
    select
        newid(),
        N'GREIS',
        N'TreatmentCourse',
        convert(nvarchar(160),c.GreisTreatmentId),
        c.DentallaTreatmentCourseId,
        sysdatetimeoffset()
    from #course_source c
    where c.ExistingExternalIdentifierId is null;

    merge migration.GreisTreatmentCourseImportMap as target
    using #course_source as source
    on target.GreisTreatmentId=source.GreisTreatmentId
    when matched then
        update set
            DentallaTreatmentCourseId=source.DentallaTreatmentCourseId,
            DentallaPatientId=source.DentallaPatientId,
            LastVerifiedAtUtc=sysdatetimeoffset()
    when not matched then
        insert
        (
            GreisTreatmentId,
            DentallaTreatmentCourseId,
            DentallaPatientId,
            ImportedAtUtc,
            LastVerifiedAtUtc
        )
        values
        (
            source.GreisTreatmentId,
            source.DentallaTreatmentCourseId,
            source.DentallaPatientId,
            sysdatetimeoffset(),
            sysdatetimeoffset()
        );

    merge clinical.TreatmentCourseEncounters as target
    using #course_encounter_source as source
    on target.TreatmentCourseId=source.DentallaTreatmentCourseId
   and target.EncounterId=source.DentallaEncounterId
    when not matched then
        insert(TreatmentCourseId,EncounterId,ImportedAtUtc)
        values(source.DentallaTreatmentCourseId,source.DentallaEncounterId,sysdatetimeoffset());

    insert into clinical.TreatmentCoursePlannedServices
    (
        Id,TreatmentCourseId,ServiceCatalogItemId,Quantity,DiscountPercent,
        LegacyToothCode,DiagnosisCode,CreatedLocal,ChangedLocal,ImportedAtUtc
    )
    select
        p.DentallaPlannedServiceId,
        p.DentallaTreatmentCourseId,
        p.DentallaServiceCatalogItemId,
        p.Quantity,
        p.DiscountPercent,
        p.LegacyToothCode,
        p.DiagnosisCode,
        p.CreatedLocal,
        p.ChangedLocal,
        sysdatetimeoffset()
    from #planned_source p
    where p.ExistingExternalIdentifierId is null;

    insert into integration.ExternalIdentifiers
    (
        Id,SystemCode,EntityType,ExternalId,InternalEntityId,ImportedAtUtc
    )
    select
        newid(),
        N'GREIS',
        N'TreatmentCoursePlannedService',
        convert(nvarchar(160),p.GreisPrescriptionId),
        p.DentallaPlannedServiceId,
        sysdatetimeoffset()
    from #planned_source p
    where p.ExistingExternalIdentifierId is null;

    merge migration.GreisTreatmentCoursePlannedServiceImportMap as target
    using #planned_source as source
    on target.GreisPrescriptionId=source.GreisPrescriptionId
    when matched then
        update set
            GreisTreatmentId=source.GreisTreatmentId,
            GreisPriceArticleId=source.GreisPriceArticleId,
            DentallaPlannedServiceId=source.DentallaPlannedServiceId,
            DentallaTreatmentCourseId=source.DentallaTreatmentCourseId,
            DentallaServiceCatalogItemId=source.DentallaServiceCatalogItemId,
            LastVerifiedAtUtc=sysdatetimeoffset()
    when not matched then
        insert
        (
            GreisPrescriptionId,
            GreisTreatmentId,
            GreisPriceArticleId,
            DentallaPlannedServiceId,
            DentallaTreatmentCourseId,
            DentallaServiceCatalogItemId,
            ImportedAtUtc,
            LastVerifiedAtUtc
        )
        values
        (
            source.GreisPrescriptionId,
            source.GreisTreatmentId,
            source.GreisPriceArticleId,
            source.DentallaPlannedServiceId,
            source.DentallaTreatmentCourseId,
            source.DentallaServiceCatalogItemId,
            sysdatetimeoffset(),
            sysdatetimeoffset()
        );

    insert into integration.LegacyTreatmentCourseEvents
    (
        Id,SystemCode,ExternalId,LegacyTreatmentId,TreatmentCourseId,
        PatientId,StaffProfileId,EventLocal,TreatmentName,OperationType,
        ChangeDescription,DoctorDisplayName,PatientDisplayName,ImportedAtUtc
    )
    select
        e.DentallaLegacyEventId,
        N'GREIS',
        convert(nvarchar(160),e.GreisHistoryId),
        e.LegacyTreatmentId,
        e.DentallaTreatmentCourseId,
        e.DentallaPatientId,
        e.DentallaStaffProfileId,
        e.EventLocal,
        e.TreatmentName,
        e.OperationType,
        e.ChangeDescription,
        e.DoctorDisplayName,
        e.PatientDisplayName,
        sysdatetimeoffset()
    from #event_source e
    where e.ExistingExternalIdentifierId is null;

    insert into integration.ExternalIdentifiers
    (
        Id,SystemCode,EntityType,ExternalId,InternalEntityId,ImportedAtUtc
    )
    select
        newid(),
        N'GREIS',
        N'LegacyTreatmentCourseEvent',
        convert(nvarchar(160),e.GreisHistoryId),
        e.DentallaLegacyEventId,
        sysdatetimeoffset()
    from #event_source e
    where e.ExistingExternalIdentifierId is null;

    merge migration.GreisTreatmentCourseEventImportMap as target
    using #event_source as source
    on target.GreisHistoryId=source.GreisHistoryId
    when matched then
        update set
            DentallaLegacyEventId=source.DentallaLegacyEventId,
            LastVerifiedAtUtc=sysdatetimeoffset()
    when not matched then
        insert
        (
            GreisHistoryId,
            DentallaLegacyEventId,
            ImportedAtUtc,
            LastVerifiedAtUtc
        )
        values
        (
            source.GreisHistoryId,
            source.DentallaLegacyEventId,
            sysdatetimeoffset(),
            sysdatetimeoffset()
        );

    if
    (
        select count(*)
        from integration.ExternalIdentifiers
        where upper(SystemCode)=N'GREIS'
          and upper(EntityType)=N'TREATMENTCOURSE'
    )<>27
        throw 55225, 'GREIS TreatmentCourse ExternalIdentifier count is not 27.', 1;

    if
    (
        select count(*)
        from integration.ExternalIdentifiers
        where upper(SystemCode)=N'GREIS'
          and upper(EntityType)=N'TREATMENTCOURSEPLANNEDSERVICE'
    )<>5
        throw 55226, 'GREIS TreatmentCoursePlannedService ExternalIdentifier count is not 5.', 1;

    if
    (
        select count(*)
        from integration.ExternalIdentifiers
        where upper(SystemCode)=N'GREIS'
          and upper(EntityType)=N'LEGACYTREATMENTCOURSEEVENT'
    )<>1437
        throw 55227, 'GREIS LegacyTreatmentCourseEvent ExternalIdentifier count is not 1,437.', 1;

    if (select count(*) from migration.GreisTreatmentCourseImportMap)<>27
        throw 55228, 'GreisTreatmentCourseImportMap count is not 27.', 1;

    if (select count(*) from migration.GreisTreatmentCoursePlannedServiceImportMap)<>5
        throw 55229, 'GreisTreatmentCoursePlannedServiceImportMap count is not 5.', 1;

    if (select count(*) from migration.GreisTreatmentCourseEventImportMap)<>1437
        throw 55230, 'GreisTreatmentCourseEventImportMap count is not 1,437.', 1;

    if
    (
        select count(*)
        from clinical.TreatmentCourseEncounters tce
        join migration.GreisTreatmentCourseImportMap cm
          on cm.DentallaTreatmentCourseId=tce.TreatmentCourseId
    )<>35
        throw 55231, 'GREIS TreatmentCourseEncounter link count is not 35.', 1;

    if exists
    (
        select 1
        from #course_source s
        join clinical.TreatmentCourses c
          on c.Id=s.DentallaTreatmentCourseId
        where c.PatientId<>s.DentallaPatientId
    )
        throw 55232, 'TreatmentCourse patient reconciliation failed.', 1;

    if
    (
        select count(*)
        from migration.GreisTreatmentCoursePlannedServiceImportMap
        where GreisTreatmentId=132
    )<>5
        throw 55233, 'Expected all five GREIS prescriptions on treatment 132.', 1;

    commit transaction;
end try
begin catch
    if @@trancount>0 rollback transaction;
    throw;
end catch;

----------------------------------------------------------------------
-- 7. RESULTS
----------------------------------------------------------------------

declare @course_ext_after bigint =
(
    select count_big(*)
    from integration.ExternalIdentifiers
    where upper(SystemCode)=N'GREIS'
      and upper(EntityType)=N'TREATMENTCOURSE'
);

declare @planned_ext_after bigint =
(
    select count_big(*)
    from integration.ExternalIdentifiers
    where upper(SystemCode)=N'GREIS'
      and upper(EntityType)=N'TREATMENTCOURSEPLANNEDSERVICE'
);

declare @event_ext_after bigint =
(
    select count_big(*)
    from integration.ExternalIdentifiers
    where upper(SystemCode)=N'GREIS'
      and upper(EntityType)=N'LEGACYTREATMENTCOURSEEVENT'
);

declare @service_ext_after bigint =
(
    select count_big(*)
    from integration.ExternalIdentifiers
    where upper(SystemCode)=N'GREIS'
      and upper(EntityType)=N'SERVICECATALOGITEM'
);

select
    @course_ext_before as CourseExternalBefore,
    @course_ext_after as CourseExternalAfter,
    @course_ext_after-@course_ext_before as CoursesCreatedThisRun,

    @planned_ext_before as PlannedServiceExternalBefore,
    @planned_ext_after as PlannedServiceExternalAfter,
    @planned_ext_after-@planned_ext_before as PlannedServicesCreatedThisRun,

    @event_ext_before as CourseEventExternalBefore,
    @event_ext_after as CourseEventExternalAfter,
    @event_ext_after-@event_ext_before as CourseEventsCreatedThisRun,

    @service_ext_before as ServiceCatalogExternalBefore,
    @service_ext_after as ServiceCatalogExternalAfter,
    @service_ext_after-@service_ext_before as ExtraCatalogItemsCreatedForPrescriptions;

select
    count(*) as GreisTreatmentCourses,
    sum(case when IsCompleted=1 then 1 else 0 end) as CompletedCourses,
    sum(case when StartLocal is null then 1 else 0 end) as CoursesWithoutStart,
    sum(case when EndLocal is null then 1 else 0 end) as CoursesWithoutEnd
from clinical.TreatmentCourses c
join integration.ExternalIdentifiers x
  on x.InternalEntityId=c.Id
 and upper(x.SystemCode)=N'GREIS'
 and upper(x.EntityType)=N'TREATMENTCOURSE';

select
    count(*) as CourseEncounterLinks,
    count(distinct TreatmentCourseId) as CoursesWithEncounterLinks,
    count(distinct EncounterId) as DistinctEncountersLinked
from clinical.TreatmentCourseEncounters l
join migration.GreisTreatmentCourseImportMap m
  on m.DentallaTreatmentCourseId=l.TreatmentCourseId;

select
    count(*) as PlannedServices,
    sum(Quantity) as PlannedQuantityTotal,
    count(distinct TreatmentCourseId) as CoursesWithPlannedServices
from clinical.TreatmentCoursePlannedServices p
join migration.GreisTreatmentCoursePlannedServiceImportMap m
  on m.DentallaPlannedServiceId=p.Id;

select
    count(*) as LegacyCourseEvents,
    sum(case when TreatmentCourseId is not null then 1 else 0 end) as EventsLinkedToCurrentCourse,
    sum(case when PatientId is not null then 1 else 0 end) as EventsWithCanonicalPatient,
    sum(case when StaffProfileId is not null then 1 else 0 end) as EventsWithCanonicalStaff
from integration.LegacyTreatmentCourseEvents
where upper(SystemCode)=N'GREIS';

select
    OperationType,
    count(*) as EventCount
from integration.LegacyTreatmentCourseEvents
where upper(SystemCode)=N'GREIS'
group by OperationType
order by EventCount desc,OperationType;

select
    m.GreisTreatmentId,
    c.Name,
    c.PatientId,
    count(l.EncounterId) as LinkedEncounters,
    (
        select count(*)
        from migration.GreisTreatmentCoursePlannedServiceImportMap ps
        where ps.GreisTreatmentId=m.GreisTreatmentId
    ) as PlannedServices
from migration.GreisTreatmentCourseImportMap m
join clinical.TreatmentCourses c
  on c.Id=m.DentallaTreatmentCourseId
left join clinical.TreatmentCourseEncounters l
  on l.TreatmentCourseId=c.Id
group by m.GreisTreatmentId,c.Name,c.PatientId
having count(l.EncounterId)=0
order by m.GreisTreatmentId;

print '=== GREIS PHASE 5B COMPLETE ===';
