using Dentalla.Contracts.Workspaces;

namespace Dentalla.Application.Workspaces;

public interface IWorkspaceScheduleReader
{
    Task<DayScheduleDto> ReadDayAsync(
        DateOnly date,
        Guid? staffProfileId,
        CancellationToken cancellationToken = default);
}
