using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentalla.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DentallaDbContext))]
[Migration("20260924150000_InitialServer")]
public partial class InitialServer : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "security");

        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CardNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                FullName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                BirthDate = table.Column<DateOnly>(type: "date", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Patients", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PermissionDefinitions",
            schema: "security",
            columns: table => new
            {
                Code = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Area = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                IsClinicalPrivilegeBound = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PermissionDefinitions", x => x.Code);
            });

        migrationBuilder.CreateTable(
            name: "UserAccounts",
            schema: "security",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StaffProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                NormalizedUserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                DisabledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserAccounts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            schema: "security",
            columns: table => new
            {
                RoleCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                PermissionCode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                IsAllowed = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => new { x.RoleCode, x.PermissionCode });
                table.ForeignKey(
                    name: "FK_RolePermissions_PermissionDefinitions_PermissionCode",
                    column: x => x.PermissionCode,
                    principalSchema: "security",
                    principalTable: "PermissionDefinitions",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DelegationGrants",
            schema: "security",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GrantedByUserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GrantedToUserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PermissionCode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                ScopeType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ScopeValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                LimitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                ValidFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ValidToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RevokedByUserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DelegationGrants", x => x.Id);
                table.ForeignKey(
                    name: "FK_DelegationGrants_PermissionDefinitions_PermissionCode",
                    column: x => x.PermissionCode,
                    principalSchema: "security",
                    principalTable: "PermissionDefinitions",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserPermissionOverrides",
            schema: "security",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PermissionCode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                ScopeType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ScopeValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                LimitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                ValidFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ValidToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                GrantedByUserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPermissionOverrides", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserPermissionOverrides_PermissionDefinitions_PermissionCode",
                    column: x => x.PermissionCode,
                    principalSchema: "security",
                    principalTable: "PermissionDefinitions",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UserPermissionOverrides_UserAccounts_UserAccountId",
                    column: x => x.UserAccountId,
                    principalSchema: "security",
                    principalTable: "UserAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoleAssignments",
            schema: "security",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                ValidFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ValidToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                GrantedByUserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoleAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserRoleAssignments_UserAccounts_UserAccountId",
                    column: x => x.UserAccountId,
                    principalSchema: "security",
                    principalTable: "UserAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Patients_CardNumber",
            table: "Patients",
            column: "CardNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DelegationGrants_GrantedToUserAccountId_PermissionCode_ValidFromUtc_ValidToUtc",
            schema: "security",
            table: "DelegationGrants",
            columns: new[] { "GrantedToUserAccountId", "PermissionCode", "ValidFromUtc", "ValidToUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_DelegationGrants_PermissionCode",
            schema: "security",
            table: "DelegationGrants",
            column: "PermissionCode");

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_PermissionCode",
            schema: "security",
            table: "RolePermissions",
            column: "PermissionCode");

        migrationBuilder.CreateIndex(
            name: "IX_UserAccounts_NormalizedUserName",
            schema: "security",
            table: "UserAccounts",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserAccounts_StaffProfileId",
            schema: "security",
            table: "UserAccounts",
            column: "StaffProfileId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissionOverrides_PermissionCode",
            schema: "security",
            table: "UserPermissionOverrides",
            column: "PermissionCode");

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissionOverrides_UserAccountId_PermissionCode_ValidFromUtc",
            schema: "security",
            table: "UserPermissionOverrides",
            columns: new[] { "UserAccountId", "PermissionCode", "ValidFromUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_UserRoleAssignments_UserAccountId_RoleCode_ValidFromUtc",
            schema: "security",
            table: "UserRoleAssignments",
            columns: new[] { "UserAccountId", "RoleCode", "ValidFromUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DelegationGrants", schema: "security");
        migrationBuilder.DropTable(name: "RolePermissions", schema: "security");
        migrationBuilder.DropTable(name: "UserPermissionOverrides", schema: "security");
        migrationBuilder.DropTable(name: "UserRoleAssignments", schema: "security");
        migrationBuilder.DropTable(name: "Patients");
        migrationBuilder.DropTable(name: "PermissionDefinitions", schema: "security");
        migrationBuilder.DropTable(name: "UserAccounts", schema: "security");
    }
}
