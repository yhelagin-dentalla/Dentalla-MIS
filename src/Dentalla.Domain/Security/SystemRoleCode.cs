namespace Dentalla.Domain.Security;

public static class SystemRoleCode
{
    public const string Doctor = "Doctor";
    public const string Administrator = "Administrator";
    public const string Marketer = "Marketer";
    public const string ChiefMedicalOfficer = "ChiefMedicalOfficer";
    public const string Director = "Director";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Doctor,
        Administrator,
        Marketer,
        ChiefMedicalOfficer,
        Director
    };
}
