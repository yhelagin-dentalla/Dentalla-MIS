using Dentalla.Application.Workspaces;

namespace Dentalla.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/workspaces/day-schedule", async (
            DateOnly? date,
            Guid? staffProfileId,
            IWorkspaceScheduleReader reader,
            CancellationToken ct) =>
        {
            var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
            return Results.Ok(await reader.ReadDayAsync(targetDate, staffProfileId, ct));
        });

        return endpoints;
    }
}
