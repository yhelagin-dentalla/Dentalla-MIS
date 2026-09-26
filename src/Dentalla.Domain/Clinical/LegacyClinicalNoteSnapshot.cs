namespace Dentalla.Domain.Clinical;

public sealed class LegacyClinicalNoteSnapshot
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid StaffProfileId { get; set; }
    public string SourceSystemCode { get; set; } = string.Empty;
    public string SourceEntityType { get; set; } = string.Empty;
    public string SourceExternalId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset ImportedAtUtc { get; set; }
}
