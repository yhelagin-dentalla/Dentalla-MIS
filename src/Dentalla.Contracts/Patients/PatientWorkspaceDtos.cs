namespace Dentalla.Contracts.Patients;

public sealed record PatientSearchItemDto(
    Guid Id,
    string CardNumber,
    string FullName,
    DateOnly? BirthDate,
    DateTime? LastAppointmentLocal);

public sealed record PatientSourceIdDto(
    string SystemCode,
    string ExternalId);

public sealed record PatientVisitDto(
    Guid AppointmentId,
    Guid? EncounterId,
    DateTime StartLocal,
    DateTime EndLocal,
    string StatusCode,
    string? DoctorName,
    int? LegacyRoomId,
    string? LegacyComment,
    int ServiceCount,
    decimal ServicesAmount);

public sealed record PatientServiceDto(
    Guid Id,
    Guid EncounterId,
    string? ServiceCode,
    string ServiceName,
    decimal Quantity,
    string? Tooth,
    decimal FinalAmount,
    string? DoctorName);

public sealed record PatientTreatmentCourseDto(
    Guid Id,
    string Name,
    bool IsCompleted,
    DateTime? StartLocal,
    DateTime? EndLocal,
    string? OwnerStaffName,
    int EncounterCount,
    int PlannedServiceCount);

public sealed record PatientHistoricalReceiptDto(
    string SourceSystem,
    string CalculationCode,
    DateOnly? PeriodStartLocal,
    DateOnly? PeriodEndLocal,
    decimal Amount,
    string CurrencyCode);

public sealed record PatientWorkspaceDto(
    Guid Id,
    string CardNumber,
    string FullName,
    DateOnly? BirthDate,
    IReadOnlyList<PatientSourceIdDto> SourceIds,
    IReadOnlyList<PatientVisitDto> Visits,
    IReadOnlyList<PatientServiceDto> Services,
    IReadOnlyList<PatientTreatmentCourseDto> TreatmentCourses,
    IReadOnlyList<PatientHistoricalReceiptDto> HistoricalReceipts,
    decimal HistoricalReceiptTotal);
