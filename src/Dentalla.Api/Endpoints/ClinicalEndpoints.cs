using System.Security.Claims;
using Dentalla.Application.Clinical;
using Dentalla.Contracts.Clinical;

namespace Dentalla.Api.Endpoints;

public static class ClinicalEndpoints
{
    public static IEndpointRouteBuilder MapClinicalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/clinical").RequireAuthorization();

        group.MapPost("/patients/{patientId:guid}/encounters", async (ClaimsPrincipal user, Guid patientId, StartEncounterRequest request, IClinicalCommands commands, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            try { return Results.Ok(await commands.StartEncounterAsync(userId, patientId, request, ct)); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        });

        group.MapGet("/encounters/{encounterId:guid}", async (ClaimsPrincipal user, Guid encounterId, IClinicalCommands commands, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            try { var result = await commands.GetEncounterAsync(userId, encounterId, ct); return result is null ? Results.NotFound() : Results.Ok(result); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        });

        group.MapPut("/encounters/{encounterId:guid}/note", async (ClaimsPrincipal user, Guid encounterId, UpdateClinicalNoteRequest request, IClinicalCommands commands, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            try { var result = await commands.UpdateNoteAsync(userId, encounterId, request, ct); return result is null ? Results.NotFound() : Results.Ok(result); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        });

        group.MapPost("/encounters/{encounterId:guid}/note/sign", async (ClaimsPrincipal user, Guid encounterId, IClinicalCommands commands, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            try { var result = await commands.SignNoteAsync(userId, encounterId, ct); return result is null ? Results.NotFound() : Results.Ok(result); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        });

        group.MapPost("/encounters/{encounterId:guid}/complete", async (ClaimsPrincipal user, Guid encounterId, IClinicalCommands commands, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            try { var result = await commands.CompleteEncounterAsync(userId, encounterId, ct); return result is null ? Results.NotFound() : Results.Ok(result); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
        => Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
}
