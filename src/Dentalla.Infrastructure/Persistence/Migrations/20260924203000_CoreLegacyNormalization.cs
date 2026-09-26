using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentalla.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DentallaDbContext))]
[Migration("20260924203000_CoreLegacyNormalization")]
public partial class CoreLegacyNormalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "integration");
        migrationBuilder.EnsureSchema(name: "scheduling");

        migrationBuilder.DropIndex(
            name: "IX_Patients_CardNumber",
            table: "Patients");

        migrationBuilder.AlterColumn<string>(
            name: "CardNumber",
            table: "Patients",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(32)",
            oldMaxLength: 32);

        migrationBuilder.CreateIndex(
            name: "IX_Patients_CardNumber",
            table: "Patients",
            column: "CardNumber");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_FullName",
            table: "Patients",
            column: "FullName");

        migrationBuilder.CreateTable(
            name: "ExternalIdentifiers",
            schema: "integration",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SystemCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ExternalId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                InternalEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ImportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExternalIdentifiers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Appointments",
            schema: "scheduling",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StaffProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                StartLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                EndLocal = table.Column<DateTime>(type: "datetime2", nullable: false),
                StatusCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                LegacyRoomId = table.Column<int>(type: "int", nullable: true),
                ImportedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appointments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Appointments_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Appointments_StaffProfiles_StaffProfileId",
                    column: x => x.StaffProfileId,
                    principalSchema: "staff",
                    principalTable: "StaffProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ExternalIdentifiers_SystemCode_EntityType_ExternalId",
            schema: "integration",
            table: "ExternalIdentifiers",
            columns: new[] { "SystemCode", "EntityType", "ExternalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExternalIdentifiers_EntityType_InternalEntityId",
            schema: "integration",
            table: "ExternalIdentifiers",
            columns: new[] { "EntityType", "InternalEntityId" });

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_StartLocal",
            schema: "scheduling",
            table: "Appointments",
            column: "StartLocal");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_StaffProfileId_StartLocal",
            schema: "scheduling",
            table: "Appointments",
            columns: new[] { "StaffProfileId", "StartLocal" });

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_PatientId_StartLocal",
            schema: "scheduling",
            table: "Appointments",
            columns: new[] { "PatientId", "StartLocal" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Appointments", schema: "scheduling");
        migrationBuilder.DropTable(name: "ExternalIdentifiers", schema: "integration");
        migrationBuilder.DropIndex(name: "IX_Patients_FullName", table: "Patients");
        migrationBuilder.DropIndex(name: "IX_Patients_CardNumber", table: "Patients");

        migrationBuilder.AlterColumn<string>(
            name: "CardNumber",
            table: "Patients",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(64)",
            oldMaxLength: 64);

        migrationBuilder.CreateIndex(
            name: "IX_Patients_CardNumber",
            table: "Patients",
            column: "CardNumber",
            unique: true);
    }
}
