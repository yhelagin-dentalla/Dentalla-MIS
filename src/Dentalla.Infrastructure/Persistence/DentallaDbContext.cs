using Dentalla.Domain.Audit;
using Dentalla.Domain.Patients;
using Dentalla.Domain.Integration;
using Dentalla.Domain.Scheduling;
using Dentalla.Domain.Security;
using Dentalla.Domain.Staff;
using Dentalla.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Persistence;

public sealed class DentallaDbContext(DbContextOptions<DentallaDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ExternalIdentifier> ExternalIdentifiers => Set<ExternalIdentifier>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
    public DbSet<DelegationGrant> DelegationGrants => Set<DelegationGrant>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(b =>
        {
            b.ToTable("Patients");
            b.HasKey(x => x.Id);
            b.Property(x => x.CardNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(300).IsRequired();
            b.HasIndex(x => x.CardNumber);
            b.HasIndex(x => x.FullName);
        });

        modelBuilder.Entity<Appointment>(b =>
        {
            b.ToTable("Appointments", "scheduling");
            b.HasKey(x => x.Id);
            b.Property(x => x.StatusCode).HasMaxLength(40).IsRequired();
            b.HasIndex(x => x.StartLocal);
            b.HasIndex(x => new { x.StaffProfileId, x.StartLocal });
            b.HasIndex(x => new { x.PatientId, x.StartLocal });
            b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StaffProfile>().WithMany().HasForeignKey(x => x.StaffProfileId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalIdentifier>(b =>
        {
            b.ToTable("ExternalIdentifiers", "integration");
            b.HasKey(x => x.Id);
            b.Property(x => x.SystemCode).HasMaxLength(40).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            b.Property(x => x.ExternalId).HasMaxLength(160).IsRequired();
            b.HasIndex(x => new { x.SystemCode, x.EntityType, x.ExternalId }).IsUnique();
            b.HasIndex(x => new { x.EntityType, x.InternalEntityId });
        });

        modelBuilder.Entity<StaffProfile>(b =>
        {
            b.ToTable("StaffProfiles", "staff");
            b.HasKey(x => x.Id);
            b.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
            b.HasIndex(x => x.DisplayName);
        });

        modelBuilder.Entity<UserAccount>(b =>
        {
            b.ToTable("UserAccounts", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.UserName).HasMaxLength(120).IsRequired();
            b.Property(x => x.NormalizedUserName).HasMaxLength(120).IsRequired();
            b.HasIndex(x => x.NormalizedUserName).IsUnique();
            b.HasIndex(x => x.StaffProfileId).IsUnique();
            b.HasOne<StaffProfile>().WithOne().HasForeignKey<UserAccount>(x => x.StaffProfileId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.PermissionOverrides).WithOne().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserCredential>(b =>
        {
            b.ToTable("UserCredentials", "security");
            b.HasKey(x => x.UserAccountId);
            b.Property(x => x.PasswordHashBase64).HasMaxLength(256).IsRequired();
            b.Property(x => x.PasswordSaltBase64).HasMaxLength(128).IsRequired();
            b.HasOne<UserAccount>().WithOne().HasForeignKey<UserCredential>(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuthSession>(b =>
        {
            b.ToTable("AuthSessions", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.TokenHashHex).HasMaxLength(64).IsRequired();
            b.Property(x => x.ClientName).HasMaxLength(200);
            b.Property(x => x.CreatedFromIp).HasMaxLength(80);
            b.HasIndex(x => x.TokenHashHex).IsUnique();
            b.HasIndex(x => new { x.UserAccountId, x.ExpiresAtUtc });
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRoleAssignment>(b =>
        {
            b.ToTable("UserRoleAssignments", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);
            b.HasIndex(x => new { x.UserAccountId, x.RoleCode, x.ValidFromUtc });
        });

        modelBuilder.Entity<PermissionDefinition>(b =>
        {
            b.ToTable("PermissionDefinitions", "security");
            b.HasKey(x => x.Code);
            b.Property(x => x.Code).HasMaxLength(160);
            b.Property(x => x.Area).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(300).IsRequired();
            b.HasData(PermissionCatalog.Definitions);
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("RolePermissions", "security");
            b.HasKey(x => new { x.RoleCode, x.PermissionCode });
            b.Property(x => x.RoleCode).HasMaxLength(80);
            b.Property(x => x.PermissionCode).HasMaxLength(160);
            b.HasOne<PermissionDefinition>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Restrict);
            b.HasData(PermissionCatalog.RoleDefaults);
        });

        modelBuilder.Entity<UserPermissionOverride>(b =>
        {
            b.ToTable("UserPermissionOverrides", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.PermissionCode).HasMaxLength(160).IsRequired();
            b.Property(x => x.ScopeType).HasMaxLength(80).IsRequired();
            b.Property(x => x.ScopeValue).HasMaxLength(300);
            b.Property(x => x.LimitAmount).HasPrecision(18, 2);
            b.Property(x => x.Reason).HasMaxLength(500);
            b.HasOne<PermissionDefinition>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.UserAccountId, x.PermissionCode, x.ValidFromUtc });
        });

        modelBuilder.Entity<DelegationGrant>(b =>
        {
            b.ToTable("DelegationGrants", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.PermissionCode).HasMaxLength(160).IsRequired();
            b.Property(x => x.ScopeType).HasMaxLength(80).IsRequired();
            b.Property(x => x.ScopeValue).HasMaxLength(300);
            b.Property(x => x.LimitAmount).HasPrecision(18, 2);
            b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            b.HasOne<PermissionDefinition>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.GrantedToUserAccountId, x.PermissionCode, x.ValidFromUtc, x.ValidToUtc });
        });

        modelBuilder.Entity<AuditEvent>(b =>
        {
            b.ToTable("AuditEvents", "audit");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).HasMaxLength(160).IsRequired();
            b.Property(x => x.PermissionCode).HasMaxLength(160);
            b.Property(x => x.EntityType).HasMaxLength(160);
            b.Property(x => x.EntityId).HasMaxLength(160);
            b.Property(x => x.RoleContext).HasMaxLength(300);
            b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            b.Property(x => x.DataJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.TraceId).HasMaxLength(100);
            b.Property(x => x.ClientIp).HasMaxLength(80);
            b.HasIndex(x => x.OccurredAtUtc);
            b.HasIndex(x => new { x.ActorUserAccountId, x.OccurredAtUtc });
            b.HasIndex(x => new { x.EventType, x.OccurredAtUtc });
        });
    }
}
