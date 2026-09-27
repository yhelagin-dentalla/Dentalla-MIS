using Dentalla.Contracts.Scheduling;

namespace Dentalla.Application.Scheduling;

public interface ISchedulingCommands
{
    Task<AppointmentEditorOptionsDto> GetEditorOptionsAsync(
        Guid actorUserAccountId,
        CancellationToken cancellationToken = default);

    Task<AppointmentCommandResultDto> CreateAsync(
        Guid actorUserAccountId,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AppointmentCommandResultDto?> UpdateAsync(
        Guid actorUserAccountId,
        Guid appointmentId,
        UpdateAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AppointmentCommandResultDto?> ConfirmAsync(
        Guid actorUserAccountId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<AppointmentCommandResultDto?> CancelAsync(
        Guid actorUserAccountId,
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}
