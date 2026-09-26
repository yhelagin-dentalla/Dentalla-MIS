using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentalla.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DentallaDbContext))]
[Migration("20260926163000_GreisHistoricalReceipts")]
public partial class GreisHistoricalReceipts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "finance");
        migrationBuilder.EnsureSchema(name: "migration");

        // Phase 6B was first applied directly through the audited, idempotent
        // GREIS import SQL. These guards baseline the existing database while
        // still creating the schema on a clean Dentalla database.
        migrationBuilder.Sql("""
IF OBJECT_ID(N'finance.PatientHistoricalReceiptTotals', N'U') IS NULL
BEGIN
    CREATE TABLE finance.PatientHistoricalReceiptTotals
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_PatientHistoricalReceiptTotals PRIMARY KEY,
        PatientId uniqueidentifier NOT NULL,
        SourceSystem nvarchar(40) NOT NULL,
        CalculationCode nvarchar(80) NOT NULL,
        PeriodStartLocal date NULL,
        PeriodEndLocal date NULL,
        Amount decimal(19,4) NOT NULL,
        CurrencyCode nvarchar(3) NOT NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT FK_PatientHistoricalReceiptTotals_Patients
            FOREIGN KEY(PatientId) REFERENCES dbo.Patients(Id)
    );

    CREATE UNIQUE INDEX UX_PatientHistoricalReceiptTotals_Source
        ON finance.PatientHistoricalReceiptTotals
        (PatientId, SourceSystem, CalculationCode);

    CREATE INDEX IX_PatientHistoricalReceiptTotals_Patient
        ON finance.PatientHistoricalReceiptTotals(PatientId);
END;

IF OBJECT_ID(N'migration.GreisPatientReceiptAudit', N'U') IS NULL
BEGIN
    CREATE TABLE migration.GreisPatientReceiptAudit
    (
        DentallaPatientId uniqueidentifier NOT NULL
            CONSTRAINT PK_GreisPatientReceiptAudit PRIMARY KEY,
        DetailedVisitReceiptAmount decimal(19,4) NOT NULL,
        LegacyFallbackAmount decimal(19,4) NOT NULL,
        AccountExternalNetAmount decimal(19,4) NOT NULL,
        TotalReceiptAmount decimal(19,4) NOT NULL,
        DetailedPaymentRows int NOT NULL,
        FallbackVisitRows int NOT NULL,
        AccountTransferRows int NOT NULL,
        PeriodStartLocal date NULL,
        PeriodEndLocal date NULL,
        CalculationCode nvarchar(80) NOT NULL,
        ImportedAtUtc datetimeoffset(7) NOT NULL,
        LastVerifiedAtUtc datetimeoffset(7) NOT NULL,

        CONSTRAINT FK_GreisPatientReceiptAudit_Patients
            FOREIGN KEY(DentallaPatientId) REFERENCES dbo.Patients(Id)
    );
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF OBJECT_ID(N'migration.GreisPatientReceiptAudit', N'U') IS NOT NULL
    DROP TABLE migration.GreisPatientReceiptAudit;

IF OBJECT_ID(N'finance.PatientHistoricalReceiptTotals', N'U') IS NOT NULL
    DROP TABLE finance.PatientHistoricalReceiptTotals;
""");
    }
}
