namespace Dentalla.Domain.Clinical;

public sealed class TreatmentCourseEncounter
{
    public Guid TreatmentCourseId { get; set; }
    public Guid EncounterId { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
