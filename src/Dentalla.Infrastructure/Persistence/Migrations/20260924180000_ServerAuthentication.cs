using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dentalla.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DentallaDbContext))]
[Migration("20260924180000_ServerAuthentication")]
public partial class ServerAuthentication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "staff");
        migrationBuilder.EnsureSchema(name: "audit");

        migrationBuilder.CreateTable(
            name: "StaffProfiles",
            schema: "staff",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                DisabledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StaffProfiles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AuditEvents",
            schema: "audit",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ActorUserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ActorSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                EventType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                PermissionCode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                EntityType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                EntityId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                RoleContext = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                DataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                TraceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ClientIp = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEvents", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserCredentials",
            schema: "security",
            columns: table => new
            {
                UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PasswordHashBase64 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                PasswordSaltBase64 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                PasswordIterations = table.Column<int>(type: "int", nullable: false),
                PasswordChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserCredentials", x => x.UserAccountId);
                table.ForeignKey(
                    name: "FK_UserCredentials_UserAccounts_UserAccountId",
                    column: x => x.UserAccountId,
                    principalSchema: "security",
                    principalTable: "UserAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AuthSessions",
            schema: "security",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHashHex = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                LastSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ClientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CreatedFromIp = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuthSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_AuthSessions_UserAccounts_UserAccountId",
                    column: x => x.UserAccountId,
                    principalSchema: "security",
                    principalTable: "UserAccounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_StaffProfiles_DisplayName",
            schema: "staff",
            table: "StaffProfiles",
            column: "DisplayName");

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_OccurredAtUtc",
            schema: "audit",
            table: "AuditEvents",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_ActorUserAccountId_OccurredAtUtc",
            schema: "audit",
            table: "AuditEvents",
            columns: new[] { "ActorUserAccountId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_EventType_OccurredAtUtc",
            schema: "audit",
            table: "AuditEvents",
            columns: new[] { "EventType", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_AuthSessions_TokenHashHex",
            schema: "security",
            table: "AuthSessions",
            column: "TokenHashHex",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuthSessions_UserAccountId_ExpiresAtUtc",
            schema: "security",
            table: "AuthSessions",
            columns: new[] { "UserAccountId", "ExpiresAtUtc" });

        migrationBuilder.AddForeignKey(
            name: "FK_UserAccounts_StaffProfiles_StaffProfileId",
            schema: "security",
            table: "UserAccounts",
            column: "StaffProfileId",
            principalSchema: "staff",
            principalTable: "StaffProfiles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_UserAccounts_StaffProfiles_StaffProfileId",
            schema: "security",
            table: "UserAccounts");

        migrationBuilder.DropTable(name: "AuthSessions", schema: "security");
        migrationBuilder.DropTable(name: "UserCredentials", schema: "security");
        migrationBuilder.DropTable(name: "AuditEvents", schema: "audit");
        migrationBuilder.DropTable(name: "StaffProfiles", schema: "staff");
    }
}
