using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentalla.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DentallaDbContext))]
[Migration("20260926093000_GreisClinicalServices")]
public partial class GreisClinicalServices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "clinical");
        migrationBuilder.EnsureSchema(name: "services");
        migrationBuilder.EnsureSchema(name: "integration");

        // Phase 4B was first applied directly to the migration database through
        // an audited, idempotent SQL script. This migration deliberately uses
        // IF OBJECT_ID guards so it can both:
        //   1) baseline that already-created schema in the current Dentalla DB;
        //   2) create the same schema on a clean database built only from EF migrations.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'services.ServiceCatalogItems', N'U') IS NULL
BEGIN
    CREATE TABLE services.ServiceCatalogItems
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_ServiceCatalogItems PRIMARY KEY,
        Name nvarchar(500) NOT NULL,
        Code nvarchar(100) NULL,
        GroupName nvarchar(300) NULL,
        IsHistorical bit NOT NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL
    );

    CREATE INDEX IX_ServiceCatalogItems_Name
        ON services.ServiceCatalogItems(Name);

    CREATE INDEX IX_ServiceCatalogItems_IsHistorical_Name
        ON services.ServiceCatalogItems(IsHistorical, Name);
END;

IF OBJECT_ID(N'clinical.Encounters', N'U') IS NULL
BEGIN
    CREATE TABLE clinical.Encounters
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Encounters PRIMARY KEY,
        PatientId uniqueidentifier NOT NULL,
        AppointmentId uniqueidentifier NOT NULL,
        StaffProfileId uniqueidentifier NOT NULL,
        StartedLocal datetime2(7) NOT NULL,
        EndedLocal datetime2(7) NOT NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT FK_Encounters_Patients
            FOREIGN KEY(PatientId) REFERENCES dbo.Patients(Id),
        CONSTRAINT FK_Encounters_Appointments
            FOREIGN KEY(AppointmentId) REFERENCES scheduling.Appointments(Id),
        CONSTRAINT FK_Encounters_StaffProfiles
            FOREIGN KEY(StaffProfileId) REFERENCES staff.StaffProfiles(Id)
    );

    CREATE UNIQUE INDEX UX_Encounters_AppointmentId
        ON clinical.Encounters(AppointmentId);

    CREATE INDEX IX_Encounters_PatientId_StartedLocal
        ON clinical.Encounters(PatientId, StartedLocal);

    CREATE INDEX IX_Encounters_StaffProfileId_StartedLocal
        ON clinical.Encounters(StaffProfileId, StartedLocal);
END;

IF OBJECT_ID(N'clinical.PerformedServices', N'U') IS NULL
BEGIN
    CREATE TABLE clinical.PerformedServices
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_PerformedServices PRIMARY KEY,
        EncounterId uniqueidentifier NOT NULL,
        PatientId uniqueidentifier NOT NULL,
        StaffProfileId uniqueidentifier NOT NULL,
        ServiceCatalogItemId uniqueidentifier NOT NULL,
        Quantity decimal(18,4) NOT NULL,
        Tooth nvarchar(50) NULL,
        DiagnosisCode nvarchar(50) NULL,
        Comment nvarchar(500) NULL,
        SourceUnitPrice decimal(19,4) NOT NULL,
        DiscountPercent decimal(9,4) NULL,
        DiscountAmount decimal(19,4) NULL,
        FinalAmount decimal(19,4) NOT NULL,
        PrimeCost decimal(19,4) NULL,
        LegacyManipulationOk bit NOT NULL,
        ComplexityId smallint NULL,
        ComplexityValue decimal(18,4) NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT FK_PerformedServices_Encounters
            FOREIGN KEY(EncounterId) REFERENCES clinical.Encounters(Id),
        CONSTRAINT FK_PerformedServices_Patients
            FOREIGN KEY(PatientId) REFERENCES dbo.Patients(Id),
        CONSTRAINT FK_PerformedServices_StaffProfiles
            FOREIGN KEY(StaffProfileId) REFERENCES staff.StaffProfiles(Id),
        CONSTRAINT FK_PerformedServices_ServiceCatalogItems
            FOREIGN KEY(ServiceCatalogItemId) REFERENCES services.ServiceCatalogItems(Id)
    );

    CREATE INDEX IX_PerformedServices_EncounterId
        ON clinical.PerformedServices(EncounterId);
    CREATE INDEX IX_PerformedServices_PatientId
        ON clinical.PerformedServices(PatientId);
    CREATE INDEX IX_PerformedServices_StaffProfileId
        ON clinical.PerformedServices(StaffProfileId);
    CREATE INDEX IX_PerformedServices_ServiceCatalogItemId
        ON clinical.PerformedServices(ServiceCatalogItemId);
END;

IF OBJECT_ID(N'integration.LegacyAppointmentDetails', N'U') IS NULL
BEGIN
    CREATE TABLE integration.LegacyAppointmentDetails
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_LegacyAppointmentDetails PRIMARY KEY,
        AppointmentId uniqueidentifier NOT NULL,
        SystemCode nvarchar(40) NOT NULL,
        ExternalId nvarchar(160) NOT NULL,
        LegacyComment nvarchar(1000) NULL,
        LegacyTreatmentId int NULL,
        LegacyDiagnosisText nvarchar(4000) NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT FK_LegacyAppointmentDetails_Appointments
            FOREIGN KEY(AppointmentId) REFERENCES scheduling.Appointments(Id)
    );

    CREATE UNIQUE INDEX UX_LegacyAppointmentDetails_Source
        ON integration.LegacyAppointmentDetails(SystemCode, ExternalId);

    CREATE INDEX IX_LegacyAppointmentDetails_AppointmentId
        ON integration.LegacyAppointmentDetails(AppointmentId);
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'integration.LegacyAppointmentDetails', N'U') IS NOT NULL
    DROP TABLE integration.LegacyAppointmentDetails;

IF OBJECT_ID(N'clinical.PerformedServices', N'U') IS NOT NULL
    DROP TABLE clinical.PerformedServices;

IF OBJECT_ID(N'clinical.Encounters', N'U') IS NOT NULL
    DROP TABLE clinical.Encounters;

IF OBJECT_ID(N'services.ServiceCatalogItems', N'U') IS NOT NULL
    DROP TABLE services.ServiceCatalogItems;
""");
    }
}
