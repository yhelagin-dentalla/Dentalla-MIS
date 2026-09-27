using Dentalla.Application.Patients;

namespace Dentalla.Api.Endpoints;

public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/patients");

        group.MapGet("/search", async (
            string? q,
            int? take,
            IPatientQueries queries,
            CancellationToken ct) =>
        {
            var rows = await queries.SearchAsync(q, take ?? 50, ct);
            return Results.Ok(rows);
        });

        group.MapGet("/{patientId:guid}/workspace", async (
            Guid patientId,
            IPatientQueries queries,
            CancellationToken ct) =>
        {
            var patient = await queries.GetWorkspaceAsync(patientId, ct);
            return patient is null ? Results.NotFound() : Results.Ok(patient);
        });

        return endpoints;
    }
}
