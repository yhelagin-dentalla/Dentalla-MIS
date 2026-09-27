using System.Security.Claims;
using Dentalla.Application.Scheduling;
using Dentalla.Contracts.Scheduling;

namespace Dentalla.Api.Endpoints;

public static class SchedulingEndpoints
{
    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/scheduling").RequireAuthorization();

        group.MapGet("/editor-options", async (
            ClaimsPrincipal user,
            ISchedulingCommands commands,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            try
            {
                return Results.Ok(await commands.GetEditorOptionsAsync(userId, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        group.MapPost("/appointments", async (
            ClaimsPrincipal user,
            CreateAppointmentRequest request,
            ISchedulingCommands commands,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            try
            {
                var result = await commands.CreateAsync(userId, request, ct);
                return Results.Created($"/api/scheduling/appointments/{result.Id:D}", result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPut("/appointments/{appointmentId:guid}", async (
            ClaimsPrincipal user,
            Guid appointmentId,
            UpdateAppointmentRequest request,
            ISchedulingCommands commands,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            try
            {
                var result = await commands.UpdateAsync(userId, appointmentId, request, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPost("/appointments/{appointmentId:guid}/confirm", async (
            ClaimsPrincipal user,
            Guid appointmentId,
            ISchedulingCommands commands,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            try
            {
                var result = await commands.ConfirmAsync(userId, appointmentId, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        group.MapPost("/appointments/{appointmentId:guid}/cancel", async (
            ClaimsPrincipal user,
            Guid appointmentId,
            ISchedulingCommands commands,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId))
                return Results.Unauthorized();

            try
            {
                var result = await commands.CancelAsync(userId, appointmentId, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
        => Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
}
