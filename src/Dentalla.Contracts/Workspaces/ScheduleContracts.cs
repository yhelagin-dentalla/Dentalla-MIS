namespace Dentalla.Contracts.Workspaces;

public sealed record DayScheduleDto(
    DateOnly Date,
    int TotalPatients,
    int ActiveStaff,
    IReadOnlyList<AppointmentListItemDto> Appointments);

public sealed record AppointmentListItemDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string CardNumber,
    Guid? StaffProfileId,
    string? DoctorName,
    DateTime StartLocal,
    DateTime EndLocal,
    string StatusCode,
    int? LegacyRoomId);
