namespace Dentalla.Contracts.Clinical;

public sealed record StartEncounterRequest(Guid AppointmentId, Guid? ResponsibleStaffProfileId);
public sealed record UpdateClinicalNoteRequest(string Text);

public sealed record ClinicalNoteDto(
    Guid Id,
    Guid EncounterId,
    string Text,
    string StatusCode,
    int Version,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SignedAtUtc);

public sealed record EncounterWorkspaceDto(
    Guid Id,
    Guid PatientId,
    Guid AppointmentId,
    Guid StaffProfileId,
    DateTime StartedLocal,
    DateTime? EndedLocal,
    string StatusCode,
    ClinicalNoteDto ClinicalNote);
