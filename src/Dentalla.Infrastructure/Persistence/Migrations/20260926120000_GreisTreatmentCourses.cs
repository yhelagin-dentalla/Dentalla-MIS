using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentalla.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DentallaDbContext))]
[Migration("20260926120000_GreisTreatmentCourses")]
public partial class GreisTreatmentCourses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "clinical");
        migrationBuilder.EnsureSchema(name: "integration");

        // Phase 5B was first applied directly through an audited, idempotent
        // SQL migration. These guards baseline the already-created schema in
        // the current Dentalla database while still creating it on a clean DB.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'clinical.TreatmentCourses', N'U') IS NULL
BEGIN
    CREATE TABLE clinical.TreatmentCourses
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_TreatmentCourses PRIMARY KEY,
        PatientId uniqueidentifier NOT NULL,
        OwnerStaffProfileId uniqueidentifier NULL,
        Name nvarchar(500) NOT NULL,
        IsCompleted bit NOT NULL,
        StartLocal datetime2(7) NULL,
        EndLocal datetime2(7) NULL,
        CreatedLocal datetime2(7) NULL,
        ChangedLocal datetime2(7) NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT FK_TreatmentCourses_Patients
            FOREIGN KEY(PatientId) REFERENCES dbo.Patients(Id),
        CONSTRAINT FK_TreatmentCourses_StaffProfiles
            FOREIGN KEY(OwnerStaffProfileId) REFERENCES staff.StaffProfiles(Id)
    );

    CREATE INDEX IX_TreatmentCourses_PatientId
        ON clinical.TreatmentCourses(PatientId);
    CREATE INDEX IX_TreatmentCourses_OwnerStaffProfileId
        ON clinical.TreatmentCourses(OwnerStaffProfileId);
    CREATE INDEX IX_TreatmentCourses_PatientId_StartLocal
        ON clinical.TreatmentCourses(PatientId, StartLocal);
END;

IF OBJECT_ID(N'clinical.TreatmentCourseEncounters', N'U') IS NULL
BEGIN
    CREATE TABLE clinical.TreatmentCourseEncounters
    (
        TreatmentCourseId uniqueidentifier NOT NULL,
        EncounterId uniqueidentifier NOT NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT PK_TreatmentCourseEncounters
            PRIMARY KEY(TreatmentCourseId, EncounterId),
        CONSTRAINT FK_TreatmentCourseEncounters_Courses
            FOREIGN KEY(TreatmentCourseId) REFERENCES clinical.TreatmentCourses(Id),
        CONSTRAINT FK_TreatmentCourseEncounters_Encounters
            FOREIGN KEY(EncounterId) REFERENCES clinical.Encounters(Id)
    );

    CREATE UNIQUE INDEX UX_TreatmentCourseEncounters_EncounterId
        ON clinical.TreatmentCourseEncounters(EncounterId);
END;

IF OBJECT_ID(N'clinical.TreatmentCoursePlannedServices', N'U') IS NULL
BEGIN
    CREATE TABLE clinical.TreatmentCoursePlannedServices
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_TreatmentCoursePlannedServices PRIMARY KEY,
        TreatmentCourseId uniqueidentifier NOT NULL,
        ServiceCatalogItemId uniqueidentifier NOT NULL,
        Quantity decimal(18,4) NOT NULL,
        DiscountPercent decimal(9,4) NULL,
        LegacyToothCode tinyint NOT NULL,
        DiagnosisCode nvarchar(50) NULL,
        CreatedLocal datetime2(7) NULL,
        ChangedLocal datetime2(7) NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT FK_TreatmentCoursePlannedServices_Courses
            FOREIGN KEY(TreatmentCourseId) REFERENCES clinical.TreatmentCourses(Id),
        CONSTRAINT FK_TreatmentCoursePlannedServices_ServiceCatalogItems
            FOREIGN KEY(ServiceCatalogItemId) REFERENCES services.ServiceCatalogItems(Id)
    );

    CREATE INDEX IX_TreatmentCoursePlannedServices_Course
        ON clinical.TreatmentCoursePlannedServices(TreatmentCourseId);
    CREATE INDEX IX_TreatmentCoursePlannedServices_Service
        ON clinical.TreatmentCoursePlannedServices(ServiceCatalogItemId);
END;

IF OBJECT_ID(N'integration.LegacyTreatmentCourseEvents', N'U') IS NULL
BEGIN
    CREATE TABLE integration.LegacyTreatmentCourseEvents
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_LegacyTreatmentCourseEvents PRIMARY KEY,
        SystemCode nvarchar(40) NOT NULL,
        ExternalId nvarchar(160) NOT NULL,
        LegacyTreatmentId int NOT NULL,
        TreatmentCourseId uniqueidentifier NULL,
        PatientId uniqueidentifier NULL,
        StaffProfileId uniqueidentifier NULL,
        EventLocal datetime2(7) NOT NULL,
        TreatmentName nvarchar(500) NOT NULL,
        OperationType nvarchar(100) NOT NULL,
        ChangeDescription nvarchar(4000) NOT NULL,
        DoctorDisplayName nvarchar(300) NULL,
        PatientDisplayName nvarchar(300) NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT FK_LegacyTreatmentCourseEvents_Course
            FOREIGN KEY(TreatmentCourseId) REFERENCES clinical.TreatmentCourses(Id),
        CONSTRAINT FK_LegacyTreatmentCourseEvents_Patient
            FOREIGN KEY(PatientId) REFERENCES dbo.Patients(Id),
        CONSTRAINT FK_LegacyTreatmentCourseEvents_Staff
            FOREIGN KEY(StaffProfileId) REFERENCES staff.StaffProfiles(Id)
    );

    CREATE UNIQUE INDEX UX_LegacyTreatmentCourseEvents_Source
        ON integration.LegacyTreatmentCourseEvents(SystemCode, ExternalId);
    CREATE INDEX IX_LegacyTreatmentCourseEvents_LegacyTreatmentId
        ON integration.LegacyTreatmentCourseEvents(LegacyTreatmentId, EventLocal);
    CREATE INDEX IX_LegacyTreatmentCourseEvents_PatientId
        ON integration.LegacyTreatmentCourseEvents(PatientId, EventLocal);
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'integration.LegacyTreatmentCourseEvents', N'U') IS NOT NULL
    DROP TABLE integration.LegacyTreatmentCourseEvents;

IF OBJECT_ID(N'clinical.TreatmentCoursePlannedServices', N'U') IS NOT NULL
    DROP TABLE clinical.TreatmentCoursePlannedServices;

IF OBJECT_ID(N'clinical.TreatmentCourseEncounters', N'U') IS NOT NULL
    DROP TABLE clinical.TreatmentCourseEncounters;

IF OBJECT_ID(N'clinical.TreatmentCourses', N'U') IS NOT NULL
    DROP TABLE clinical.TreatmentCourses;
""");
    }
}
