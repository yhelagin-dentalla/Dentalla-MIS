namespace Dentalla.Domain.Integration;

public sealed class LegacyTreatmentCourseEvent
{
    public Guid Id { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public int LegacyTreatmentId { get; set; }
    public Guid? TreatmentCourseId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? StaffProfileId { get; set; }
    public DateTime EventLocal { get; set; }
    public string TreatmentName { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
    public string? DoctorDisplayName { get; set; }
    public string? PatientDisplayName { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
