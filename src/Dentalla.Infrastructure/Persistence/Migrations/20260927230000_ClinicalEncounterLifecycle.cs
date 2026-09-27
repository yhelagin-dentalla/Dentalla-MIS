using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentalla.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DentallaDbContext))]
[Migration("20260927230000_ClinicalEncounterLifecycle")]
public partial class ClinicalEncounterLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(name: "EndedLocal", schema: "clinical", table: "Encounters", type: "datetime2(7)", nullable: true, oldClrType: typeof(DateTime), oldType: "datetime2(7)");
        migrationBuilder.AddColumn<Guid>(name: "OpenedByUserAccountId", schema: "clinical", table: "Encounters", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "StatusCode", schema: "clinical", table: "Encounters", type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Completed");
        migrationBuilder.CreateIndex(name: "IX_Encounters_OpenedByUserAccountId", schema: "clinical", table: "Encounters", column: "OpenedByUserAccountId");
        migrationBuilder.CreateIndex(name: "IX_Encounters_PatientId_StatusCode", schema: "clinical", table: "Encounters", columns: new[] { "PatientId", "StatusCode" });
        migrationBuilder.AddForeignKey(name: "FK_Encounters_UserAccounts_OpenedByUserAccountId", schema: "clinical", table: "Encounters", column: "OpenedByUserAccountId", principalSchema: "security", principalTable: "UserAccounts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.CreateTable(name: "ClinicalNotes", schema: "clinical", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), EncounterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), AuthorStaffProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Text = table.Column<string>(type: "nvarchar(max)", nullable: false), StatusCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), Version = table.Column<int>(type: "int", nullable: false), CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false), UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false), SignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true), SignedByUserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true) }, constraints: table => { table.PrimaryKey("PK_ClinicalNotes", x => x.Id); table.ForeignKey("FK_ClinicalNotes_Encounters_EncounterId", x => x.EncounterId, "clinical", "Encounters", "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_ClinicalNotes_Patients_PatientId", x => x.PatientId, principalTable: "Patients", principalColumn: "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_ClinicalNotes_StaffProfiles_AuthorStaffProfileId", x => x.AuthorStaffProfileId, "staff", "StaffProfiles", "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey("FK_ClinicalNotes_UserAccounts_SignedByUserAccountId", x => x.SignedByUserAccountId, "security", "UserAccounts", "Id", onDelete: ReferentialAction.Restrict); });
        migrationBuilder.CreateIndex("IX_ClinicalNotes_EncounterId", "ClinicalNotes", "EncounterId", schema: "clinical", unique: true); migrationBuilder.CreateIndex("IX_ClinicalNotes_AuthorStaffProfileId", "ClinicalNotes", "AuthorStaffProfileId", schema: "clinical"); migrationBuilder.CreateIndex("IX_ClinicalNotes_SignedByUserAccountId", "ClinicalNotes", "SignedByUserAccountId", schema: "clinical"); migrationBuilder.CreateIndex("IX_ClinicalNotes_PatientId_UpdatedAtUtc", "ClinicalNotes", new[] { "PatientId", "UpdatedAtUtc" }, schema: "clinical");
        migrationBuilder.Sql("""
IF NOT EXISTS (SELECT 1 FROM security.RolePermissions WHERE RoleCode = N'Administrator' AND PermissionCode = N'Clinical.Record.View')
    INSERT INTO security.RolePermissions(RoleCode, PermissionCode, IsAllowed) VALUES (N'Administrator', N'Clinical.Record.View', 1);
IF NOT EXISTS (SELECT 1 FROM security.RolePermissions WHERE RoleCode = N'Administrator' AND PermissionCode = N'Clinical.Note.Edit')
    INSERT INTO security.RolePermissions(RoleCode, PermissionCode, IsAllowed) VALUES (N'Administrator', N'Clinical.Note.Edit', 1);
""");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM security.RolePermissions WHERE RoleCode=N'Administrator' AND PermissionCode IN (N'Clinical.Record.View',N'Clinical.Note.Edit');");
        migrationBuilder.DropTable(name: "ClinicalNotes", schema: "clinical"); migrationBuilder.DropForeignKey(name: "FK_Encounters_UserAccounts_OpenedByUserAccountId", schema: "clinical", table: "Encounters"); migrationBuilder.DropIndex(name: "IX_Encounters_OpenedByUserAccountId", schema: "clinical", table: "Encounters"); migrationBuilder.DropIndex(name: "IX_Encounters_PatientId_StatusCode", schema: "clinical", table: "Encounters"); migrationBuilder.DropColumn(name: "OpenedByUserAccountId", schema: "clinical", table: "Encounters"); migrationBuilder.DropColumn(name: "StatusCode", schema: "clinical", table: "Encounters"); migrationBuilder.AlterColumn<DateTime>(name: "EndedLocal", schema: "clinical", table: "Encounters", type: "datetime2(7)", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime2(7)", oldNullable: true);
    }
}
