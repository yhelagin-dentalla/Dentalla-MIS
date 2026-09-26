namespace Dentalla.Infrastructure.Configuration;

public sealed class DentallaServerOptions
{
    public const string SectionName = "DentallaServer";

    public string Mode { get; set; } = "LocalServer";
    public string FileStorageRoot { get; set; } = @"C:\ProgramData\Dentalla\Storage";
    public bool EnableLanEndpoints { get; set; }
    public string ClinicInstanceName { get; set; } = "Dentalla";

    // Development/server-install bootstrap only. Production deployment will run
    // migrations as a controlled server deployment step before the new build is activated.
    public bool ApplyDatabaseMigrationsOnStartup { get; set; }
    public bool SeedReferenceDataOnStartup { get; set; } = true;
    public int LocalAuthSessionLifetimeHours { get; set; } = 12;

    // Temporary development mapping while ChiefMedicalOfficer assignments are not yet managed in MIS UI.
    // Value is an IDENT Staffs.ID_Persons legacy id, not a Dentalla user id.
    public int? DevelopmentChiefMedicalOfficerLegacyStaffId { get; set; } = 1;
}
