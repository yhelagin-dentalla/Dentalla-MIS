using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentalla.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClinicalEncounterLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GrantedByUserAccountId",
                schema: "security",
                table: "UserRoleAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidToUtc",
                schema: "security",
                table: "UserRoleAssignments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GrantedByUserAccountId",
                schema: "security",
                table: "UserPermissionOverrides",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsAllowed",
                schema: "security",
                table: "UserPermissionOverrides",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidToUtc",
                schema: "security",
                table: "UserPermissionOverrides",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordChangedAtUtc",
                schema: "security",
                table: "UserCredentials",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "PasswordIterations",
                schema: "security",
                table: "UserCredentials",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                schema: "security",
                table: "UserAccounts",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledAtUtc",
                schema: "security",
                table: "UserAccounts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "security",
                table: "UserAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangedLocal",
                schema: "clinical",
                table: "TreatmentCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedLocal",
                schema: "clinical",
                table: "TreatmentCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndLocal",
                schema: "clinical",
                table: "TreatmentCourses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "TreatmentCourses",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                schema: "clinical",
                table: "TreatmentCourses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ChangedLocal",
                schema: "clinical",
                table: "TreatmentCoursePlannedServices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedLocal",
                schema: "clinical",
                table: "TreatmentCoursePlannedServices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "TreatmentCoursePlannedServices",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<byte>(
                name: "LegacyToothCode",
                schema: "clinical",
                table: "TreatmentCoursePlannedServices",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "TreatmentCourseEncounters",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                schema: "staff",
                table: "StaffProfiles",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledAtUtc",
                schema: "staff",
                table: "StaffProfiles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "staff",
                table: "StaffProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "services",
                table: "ServiceCatalogItems",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsAllowed",
                schema: "security",
                table: "RolePermissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsClinicalPrivilegeBound",
                schema: "security",
                table: "PermissionDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSensitive",
                schema: "security",
                table: "PermissionDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "ComplexityId",
                schema: "clinical",
                table: "PerformedServices",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "PerformedServices",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "LegacyManipulationOk",
                schema: "clinical",
                table: "PerformedServices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "Patients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "finance",
                table: "PatientHistoricalReceiptTotals",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodEndLocal",
                schema: "finance",
                table: "PatientHistoricalReceiptTotals",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PeriodStartLocal",
                schema: "finance",
                table: "PatientHistoricalReceiptTotals",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "integration",
                table: "LegacyTreatmentCourseEvents",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "integration",
                table: "LegacyAppointmentDetails",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "LegacyTreatmentId",
                schema: "integration",
                table: "LegacyAppointmentDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "integration",
                table: "ExternalIdentifiers",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedLocal",
                schema: "clinical",
                table: "Encounters",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "Encounters",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "GrantedByUserAccountId",
                schema: "security",
                table: "DelegationGrants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAtUtc",
                schema: "security",
                table: "DelegationGrants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevokedByUserAccountId",
                schema: "security",
                table: "DelegationGrants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                schema: "clinical",
                table: "ClinicalNotes",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedAtUtc",
                schema: "clinical",
                table: "ClinicalNotes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "clinical",
                table: "ClinicalNotes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                schema: "security",
                table: "AuthSessions",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenAtUtc",
                schema: "security",
                table: "AuthSessions",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAtUtc",
                schema: "security",
                table: "AuthSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActorSessionId",
                schema: "audit",
                table: "AuditEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndLocal",
                schema: "scheduling",
                table: "Appointments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAtUtc",
                schema: "scheduling",
                table: "Appointments",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "LegacyRoomId",
                schema: "scheduling",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Appointment.Manage",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Appointment.View",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Audit.View",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "CashShift.Close",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Clinical.Diagnosis.Manage",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { true, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Clinical.Note.Edit",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Clinical.Note.Sign",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { true, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Clinical.Record.View",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Clinical.TreatmentPlan.Manage",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { true, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Discount.Apply",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Discount.Approve",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "ExternalIntegration.Manage",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Marketing.Campaign.Execute",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Marketing.Manage",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Marketing.View",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Patient.EditDemographics",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Patient.MergeDuplicates",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Patient.View",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Payment.Accept",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Payroll.Approve",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Payroll.ViewAll",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Payroll.ViewOwn",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Price.Publish",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Quality.Manage",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Quality.View",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "RBAC.Delegate",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "RBAC.ManageRoleProfile",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "RBAC.ManageUserOverride",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "RBAC.SwitchRoleContext",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Refund.Approve",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Refund.Create",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "ScheduleOperations.BatchBlock",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Sms.SendFreeText",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Sms.SendTemplate",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, false });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "Staff.Manage",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "PermissionDefinitions",
                keyColumn: "Code",
                keyValue: "SystemSettings.Manage",
                columns: new[] { "IsClinicalPrivilegeBound", "IsSensitive" },
                values: new object[] { false, true });

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Appointment.Manage", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Appointment.View", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "CashShift.Close", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Note.Edit", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Record.View", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Discount.Apply", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Patient.EditDemographics", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Patient.View", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Payment.Accept", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Refund.Create", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "ScheduleOperations.BatchBlock", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Sms.SendFreeText", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Sms.SendTemplate", "Administrator" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Appointment.View", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Diagnosis.Manage", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Note.Edit", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Note.Sign", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Record.View", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.TreatmentPlan.Manage", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Patient.View", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Payroll.ViewOwn", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Quality.Manage", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Quality.View", "ChiefMedicalOfficer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Appointment.Manage", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Appointment.View", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Audit.View", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "CashShift.Close", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Diagnosis.Manage", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Note.Edit", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Note.Sign", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Record.View", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.TreatmentPlan.Manage", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Discount.Apply", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Discount.Approve", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "ExternalIntegration.Manage", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Marketing.Campaign.Execute", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Marketing.Manage", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Marketing.View", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Patient.EditDemographics", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Patient.MergeDuplicates", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Patient.View", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Payment.Accept", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Payroll.Approve", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Payroll.ViewAll", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Payroll.ViewOwn", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Price.Publish", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Quality.Manage", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Quality.View", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "RBAC.Delegate", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "RBAC.ManageRoleProfile", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "RBAC.ManageUserOverride", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "RBAC.SwitchRoleContext", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Refund.Approve", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Refund.Create", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "ScheduleOperations.BatchBlock", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Sms.SendFreeText", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Sms.SendTemplate", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Staff.Manage", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "SystemSettings.Manage", "Director" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Appointment.View", "Doctor" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Diagnosis.Manage", "Doctor" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Note.Edit", "Doctor" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Note.Sign", "Doctor" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Record.View", "Doctor" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.TreatmentPlan.Manage", "Doctor" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Patient.View", "Doctor" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Payroll.ViewOwn", "Doctor" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Appointment.View", "Marketer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Clinical.Record.View", "Marketer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Marketing.Manage", "Marketer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Marketing.View", "Marketer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.UpdateData(
                schema: "security",
                table: "RolePermissions",
                keyColumns: new[] { "PermissionCode", "RoleCode" },
                keyValues: new object[] { "Patient.View", "Marketer" },
                column: "IsAllowed",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_PermissionCode",
                schema: "security",
                table: "UserPermissionOverrides",
                column: "PermissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionCode",
                schema: "security",
                table: "RolePermissions",
                column: "PermissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTreatmentCourseEvents_StaffProfileId",
                schema: "integration",
                table: "LegacyTreatmentCourseEvents",
                column: "StaffProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyTreatmentCourseEvents_TreatmentCourseId",
                schema: "integration",
                table: "LegacyTreatmentCourseEvents",
                column: "TreatmentCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_OpenedByUserAccountId",
                schema: "clinical",
                table: "Encounters",
                column: "OpenedByUserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegationGrants_PermissionCode",
                schema: "security",
                table: "DelegationGrants",
                column: "PermissionCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_AuthorStaffProfileId",
                schema: "clinical",
                table: "ClinicalNotes",
                column: "AuthorStaffProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_SignedByUserAccountId",
                schema: "clinical",
                table: "ClinicalNotes",
                column: "SignedByUserAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPermissionOverrides_PermissionCode",
                schema: "security",
                table: "UserPermissionOverrides");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PermissionCode",
                schema: "security",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_LegacyTreatmentCourseEvents_StaffProfileId",
                schema: "integration",
                table: "LegacyTreatmentCourseEvents");

            migrationBuilder.DropIndex(
                name: "IX_LegacyTreatmentCourseEvents_TreatmentCourseId",
                schema: "integration",
                table: "LegacyTreatmentCourseEvents");

            migrationBuilder.DropIndex(
                name: "IX_Encounters_OpenedByUserAccountId",
                schema: "clinical",
                table: "Encounters");

            migrationBuilder.DropIndex(
                name: "IX_DelegationGrants_PermissionCode",
                schema: "security",
                table: "DelegationGrants");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalNotes_AuthorStaffProfileId",
                schema: "clinical",
                table: "ClinicalNotes");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalNotes_SignedByUserAccountId",
                schema: "clinical",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "GrantedByUserAccountId",
                schema: "security",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "ValidToUtc",
                schema: "security",
                table: "UserRoleAssignments");

            migrationBuilder.DropColumn(
                name: "GrantedByUserAccountId",
                schema: "security",
                table: "UserPermissionOverrides");

            migrationBuilder.DropColumn(
                name: "IsAllowed",
                schema: "security",
                table: "UserPermissionOverrides");

            migrationBuilder.DropColumn(
                name: "ValidToUtc",
                schema: "security",
                table: "UserPermissionOverrides");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAtUtc",
                schema: "security",
                table: "UserCredentials");

            migrationBuilder.DropColumn(
                name: "PasswordIterations",
                schema: "security",
                table: "UserCredentials");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "security",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "DisabledAtUtc",
                schema: "security",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "security",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "ChangedLocal",
                schema: "clinical",
                table: "TreatmentCourses");

            migrationBuilder.DropColumn(
                name: "CreatedLocal",
                schema: "clinical",
                table: "TreatmentCourses");

            migrationBuilder.DropColumn(
                name: "EndLocal",
                schema: "clinical",
                table: "TreatmentCourses");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "TreatmentCourses");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                schema: "clinical",
                table: "TreatmentCourses");

            migrationBuilder.DropColumn(
                name: "ChangedLocal",
                schema: "clinical",
                table: "TreatmentCoursePlannedServices");

            migrationBuilder.DropColumn(
                name: "CreatedLocal",
                schema: "clinical",
                table: "TreatmentCoursePlannedServices");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "TreatmentCoursePlannedServices");

            migrationBuilder.DropColumn(
                name: "LegacyToothCode",
                schema: "clinical",
                table: "TreatmentCoursePlannedServices");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "TreatmentCourseEncounters");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "staff",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "DisabledAtUtc",
                schema: "staff",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "staff",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "services",
                table: "ServiceCatalogItems");

            migrationBuilder.DropColumn(
                name: "IsAllowed",
                schema: "security",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "IsClinicalPrivilegeBound",
                schema: "security",
                table: "PermissionDefinitions");

            migrationBuilder.DropColumn(
                name: "IsSensitive",
                schema: "security",
                table: "PermissionDefinitions");

            migrationBuilder.DropColumn(
                name: "ComplexityId",
                schema: "clinical",
                table: "PerformedServices");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "PerformedServices");

            migrationBuilder.DropColumn(
                name: "LegacyManipulationOk",
                schema: "clinical",
                table: "PerformedServices");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "finance",
                table: "PatientHistoricalReceiptTotals");

            migrationBuilder.DropColumn(
                name: "PeriodEndLocal",
                schema: "finance",
                table: "PatientHistoricalReceiptTotals");

            migrationBuilder.DropColumn(
                name: "PeriodStartLocal",
                schema: "finance",
                table: "PatientHistoricalReceiptTotals");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "integration",
                table: "LegacyTreatmentCourseEvents");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "integration",
                table: "LegacyAppointmentDetails");

            migrationBuilder.DropColumn(
                name: "LegacyTreatmentId",
                schema: "integration",
                table: "LegacyAppointmentDetails");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "integration",
                table: "ExternalIdentifiers");

            migrationBuilder.DropColumn(
                name: "EndedLocal",
                schema: "clinical",
                table: "Encounters");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "clinical",
                table: "Encounters");

            migrationBuilder.DropColumn(
                name: "GrantedByUserAccountId",
                schema: "security",
                table: "DelegationGrants");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                schema: "security",
                table: "DelegationGrants");

            migrationBuilder.DropColumn(
                name: "RevokedByUserAccountId",
                schema: "security",
                table: "DelegationGrants");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "clinical",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "SignedAtUtc",
                schema: "clinical",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "clinical",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "security",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "LastSeenAtUtc",
                schema: "security",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                schema: "security",
                table: "AuthSessions");

            migrationBuilder.DropColumn(
                name: "ActorSessionId",
                schema: "audit",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "EndLocal",
                schema: "scheduling",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                schema: "scheduling",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "LegacyRoomId",
                schema: "scheduling",
                table: "Appointments");
        }
    }
}
