namespace Dentalla.Contracts.Scheduling;

public sealed record AppointmentStaffOptionDto(Guid Id, string DisplayName);

public sealed record AppointmentEditorOptionsDto(
    IReadOnlyList<AppointmentStaffOptionDto> Doctors,
    IReadOnlyList<int> Rooms);

public sealed record CreateAppointmentRequest(
    Guid PatientId,
    Guid StaffProfileId,
    DateTime StartLocal,
    DateTime EndLocal,
    int? RoomId);

public sealed record UpdateAppointmentRequest(
    Guid StaffProfileId,
    DateTime StartLocal,
    DateTime EndLocal,
    int? RoomId);

public sealed record AppointmentCommandResultDto(
    Guid Id,
    Guid PatientId,
    Guid? StaffProfileId,
    DateTime StartLocal,
    DateTime EndLocal,
    string StatusCode,
    int? RoomId);
