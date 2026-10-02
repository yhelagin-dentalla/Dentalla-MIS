using Dentalla.Domain.Audit;
using Dentalla.Domain.Clinical;
using Dentalla.Domain.Finance;
using Dentalla.Domain.Patients;
using Dentalla.Domain.Integration;
using Dentalla.Domain.Scheduling;
using Dentalla.Domain.Security;
using Dentalla.Domain.Services;
using Dentalla.Domain.Staff;
using Dentalla.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Dentalla.Infrastructure.Persistence;

public sealed class DentallaDbContext(DbContextOptions<DentallaDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientHistoricalReceiptTotal> PatientHistoricalReceiptTotals => Set<PatientHistoricalReceiptTotal>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<PerformedService> PerformedServices => Set<PerformedService>();
    public DbSet<TreatmentCourse> TreatmentCourses => Set<TreatmentCourse>();
    public DbSet<TreatmentCourseEncounter> TreatmentCourseEncounters => Set<TreatmentCourseEncounter>();
    public DbSet<TreatmentCoursePlannedService> TreatmentCoursePlannedServices => Set<TreatmentCoursePlannedService>();
    public DbSet<ServiceCatalogItem> ServiceCatalogItems => Set<ServiceCatalogItem>();
    public DbSet<ExternalIdentifier> ExternalIdentifiers => Set<ExternalIdentifier>();
    public DbSet<LegacyAppointmentDetail> LegacyAppointmentDetails => Set<LegacyAppointmentDetail>();
    public DbSet<LegacyTreatmentCourseEvent> LegacyTreatmentCourseEvents => Set<LegacyTreatmentCourseEvent>();
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
        modelBuilder.Entity<Person>(b =>
        {
            b.ToTable("Persons");
            b.HasKey(x => x.Id);
            b.Property(x => x.LastName).HasMaxLength(150).IsRequired();
            b.Property(x => x.FirstName).HasMaxLength(150).IsRequired();
            b.Property(x => x.MiddleName).HasMaxLength(150).IsRequired();
            b.Ignore(x => x.DisplayName);
            b.HasIndex(x => new { x.LastName, x.FirstName, x.MiddleName, x.BirthDate });
        });

        modelBuilder.Entity<Patient>(b =>
        {
            b.ToTable("Patients");
            b.HasKey(x => x.Id);
            b.Property(x => x.CardNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(300).IsRequired();
            b.HasIndex(x => x.CardNumber);
            b.HasIndex(x => x.FullName);
            b.HasIndex(x => x.PersonId).IsUnique().HasFilter("[PersonId] IS NOT NULL");
            b.HasOne<Person>().WithOne().HasForeignKey<Patient>(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PatientHistoricalReceiptTotal>(b =>
        {
            b.ToTable("PatientHistoricalReceiptTotals", "finance");
            b.HasKey(x => x.Id);
            b.Property(x => x.SourceSystem).HasMaxLength(40).IsRequired();
            b.Property(x => x.CalculationCode).HasMaxLength(80).IsRequired();
            b.Property(x => x.Amount).HasPrecision(19, 4);
            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => new { x.PatientId, x.SourceSystem, x.CalculationCode }).IsUnique();
            b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Encounter>(b =>
        {
            b.ToTable("Encounters", "clinical");
            b.HasKey(x => x.Id);
            b.Property(x => x.StatusCode).HasMaxLength(40).IsRequired();
            b.HasIndex(x => x.AppointmentId).IsUnique();
            b.HasIndex(x => new { x.PatientId, x.StartedLocal });
            b.HasIndex(x => new { x.StaffProfileId, x.StartedLocal });
            b.HasIndex(x => new { x.PatientId, x.StatusCode });
            b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Appointment>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StaffProfile>().WithMany().HasForeignKey(x => x.StaffProfileId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.OpenedByUserAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClinicalNote>(b =>
        {
            b.ToTable("ClinicalNotes", "clinical");
            b.HasKey(x => x.Id);
            b.Property(x => x.Text).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.StatusCode).HasMaxLength(40).IsRequired();
            b.HasIndex(x => x.EncounterId).IsUnique();
            b.HasIndex(x => new { x.PatientId, x.UpdatedAtUtc });
            b.HasOne<Encounter>().WithMany().HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StaffProfile>().WithMany().HasForeignKey(x => x.AuthorStaffProfileId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.SignedByUserAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceCatalogItem>(b =>
        {
            b.ToTable("ServiceCatalogItems", "services");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(500).IsRequired();
            b.Property(x => x.Code).HasMaxLength(100);
            b.Property(x => x.GroupName).HasMaxLength(300);
            b.HasIndex(x => x.Name);
            b.HasIndex(x => new { x.IsHistorical, x.Name });
        });

        modelBuilder.Entity<PerformedService>(b =>
        {
            b.ToTable("PerformedServices", "clinical");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.Tooth).HasMaxLength(50);
            b.Property(x => x.DiagnosisCode).HasMaxLength(50);
            b.Property(x => x.Comment).HasMaxLength(500);
            b.Property(x => x.SourceUnitPrice).HasPrecision(19, 4);
            b.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            b.Property(x => x.DiscountAmount).HasPrecision(19, 4);
            b.Property(x => x.FinalAmount).HasPrecision(19, 4);
            b.Property(x => x.PrimeCost).HasPrecision(19, 4);
            b.Property(x => x.ComplexityValue).HasPrecision(18, 4);
            b.HasIndex(x => x.EncounterId);
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.StaffProfileId);
            b.HasIndex(x => x.ServiceCatalogItemId);
            b.HasOne<Encounter>().WithMany().HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StaffProfile>().WithMany().HasForeignKey(x => x.StaffProfileId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ServiceCatalogItem>().WithMany().HasForeignKey(x => x.ServiceCatalogItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TreatmentCourse>(b =>
        {
            b.ToTable("TreatmentCourses", "clinical");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(500).IsRequired();
            b.HasIndex(x => x.PatientId);
            b.HasIndex(x => x.OwnerStaffProfileId);
            b.HasIndex(x => new { x.PatientId, x.StartLocal });
            b.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StaffProfile>().WithMany().HasForeignKey(x => x.OwnerStaffProfileId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TreatmentCourseEncounter>(b =>
        {
            b.ToTable("TreatmentCourseEncounters", "clinical");
            b.HasKey(x => new { x.TreatmentCourseId, x.EncounterId });
            b.HasIndex(x => x.EncounterId).IsUnique();
            b.HasOne<TreatmentCourse>().WithMany().HasForeignKey(x => x.TreatmentCourseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Encounter>().WithMany().HasForeignKey(x => x.EncounterId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TreatmentCoursePlannedService>(b =>
        {
            b.ToTable("TreatmentCoursePlannedServices", "clinical");
            b.HasKey(x => x.Id);
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.DiscountPercent).HasPrecision(9, 4);
            b.Property(x => x.DiagnosisCode).HasMaxLength(50);
            b.HasIndex(x => x.TreatmentCourseId);
            b.HasIndex(x => x.ServiceCatalogItemId);
            b.HasOne<TreatmentCourse>().WithMany().HasForeignKey(x => x.TreatmentCourseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ServiceCatalogItem>().WithMany().HasForeignKey(x => x.ServiceCatalogItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalIdentifier>(b =>
        {
            b.ToTable("ExternalIdentifiers", "integration");
            b.HasKey(x => x.Id);
            b.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            b.Property(x => x.SourceSystem).HasMaxLength(80).IsRequired();
            b.Property(x => x.ExternalId).HasMaxLength(200).IsRequired();
            b.HasIndex(x => new { x.EntityType, x.SourceSystem, x.ExternalId }).IsUnique();
            b.HasIndex(x => new { x.EntityType, x.EntityId });
        });

        modelBuilder.Entity<LegacyAppointmentDetail>(b =>
        {
            b.ToTable("LegacyAppointmentDetails", "integration");
            b.HasKey(x => x.Id);
            b.Property(x => x.SourceSystem).HasMaxLength(80).IsRequired();
            b.Property(x => x.LegacyAppointmentId).HasMaxLength(200).IsRequired();
            b.Property(x => x.LegacyReceptionId).HasMaxLength(200);
            b.Property(x => x.CabinetName).HasMaxLength(300);
            b.Property(x => x.StatusText).HasMaxLength(300);
            b.Property(x => x.Comment).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.AppointmentId).IsUnique();
            b.HasIndex(x => new { x.SourceSystem, x.LegacyAppointmentId }).IsUnique();
            b.HasOne<Appointment>().WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LegacyTreatmentCourseEvent>(b =>
        {
            b.ToTable("LegacyTreatmentCourseEvents", "integration");
            b.HasKey(x => x.Id);
            b.Property(x => x.SourceSystem).HasMaxLength(80).IsRequired();
            b.Property(x => x.LegacyCourseId).HasMaxLength(200).IsRequired();
            b.Property(x => x.EventType).HasMaxLength(80).IsRequired();
            b.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
            b.HasIndex(x => x.TreatmentCourseId);
            b.HasIndex(x => new { x.SourceSystem, x.LegacyCourseId });
            b.HasOne<TreatmentCourse>().WithMany().HasForeignKey(x => x.TreatmentCourseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StaffProfile>(b =>
        {
            b.ToTable("StaffProfiles", "staff");
            b.HasKey(x => x.Id);
            b.Property(x => x.FullName).HasMaxLength(300).IsRequired();
            b.Property(x => x.Specialty).HasMaxLength(300);
            b.HasIndex(x => x.FullName);
        });

        modelBuilder.Entity<UserAccount>(b =>
        {
            b.ToTable("UserAccounts", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.Login).HasMaxLength(100).IsRequired();
            b.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
            b.HasIndex(x => x.Login).IsUnique();
            b.HasOne<StaffProfile>().WithMany().HasForeignKey(x => x.StaffProfileId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserCredential>(b =>
        {
            b.ToTable("UserCredentials", "security");
            b.HasKey(x => x.UserAccountId);
            b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            b.Property(x => x.PasswordSalt).HasMaxLength(512).IsRequired();
            b.Property(x => x.Algorithm).HasMaxLength(100).IsRequired();
            b.HasOne<UserAccount>().WithOne().HasForeignKey<UserCredential>(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuthSession>(b =>
        {
            b.ToTable("AuthSessions", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            b.Property(x => x.RevokedReason).HasMaxLength(300);
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => new { x.UserAccountId, x.ExpiresAtUtc });
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRoleAssignment>(b =>
        {
            b.ToTable("UserRoleAssignments", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoleCode).HasMaxLength(80).IsRequired();
            b.HasIndex(x => new { x.UserAccountId, x.RoleCode }).IsUnique();
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PermissionDefinition>(b =>
        {
            b.ToTable("PermissionDefinitions", "security");
            b.HasKey(x => x.Code);
            b.Property(x => x.Code).HasMaxLength(160);
            b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("RolePermissions", "security");
            b.HasKey(x => new { x.RoleCode, x.PermissionCode });
            b.Property(x => x.RoleCode).HasMaxLength(80);
            b.Property(x => x.PermissionCode).HasMaxLength(160);
            b.HasOne<PermissionDefinition>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPermissionOverride>(b =>
        {
            b.ToTable("UserPermissionOverrides", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.PermissionCode).HasMaxLength(160).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);
            b.HasIndex(x => new { x.UserAccountId, x.PermissionCode }).IsUnique();
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<PermissionDefinition>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DelegationGrant>(b =>
        {
            b.ToTable("DelegationGrants", "security");
            b.HasKey(x => x.Id);
            b.Property(x => x.PermissionCode).HasMaxLength(160).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);
            b.HasIndex(x => new { x.GranteeUserAccountId, x.ValidFromUtc, x.ValidToUtc });
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.GrantorUserAccountId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.GranteeUserAccountId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<PermissionDefinition>().WithMany().HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEvent>(b =>
        {
            b.ToTable("AuditEvents", "audit");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(120).IsRequired();
            b.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
            b.HasIndex(x => x.OccurredAtUtc);
            b.HasIndex(x => new { x.EntityType, x.EntityId });
            b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.ActorUserAccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
