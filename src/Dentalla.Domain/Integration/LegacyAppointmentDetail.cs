namespace Dentalla.Domain.Integration;

/// <summary>
/// Source-aware provenance attached to an imported legacy appointment.
/// This is deliberately not a ClinicalNote: legacy scheduling comments may
/// contain administrative callbacks, cancellation notes, promotions, etc.
/// </summary>
public sealed class LegacyAppointmentDetail
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? LegacyComment { get; set; }
    public int? LegacyTreatmentId { get; set; }
    public string? LegacyDiagnosisText { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
}
