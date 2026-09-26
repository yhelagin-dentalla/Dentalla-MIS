using Dentalla.Application.Abstractions;
using Dentalla.Infrastructure.Configuration;
using Dentalla.Infrastructure.Health;
using Dentalla.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Dentalla.Api.Endpoints;

public static class ServerEndpoints
{
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new
        {
            status = "live",
            service = "Dentalla.Server",
            utc = DateTimeOffset.UtcNow
        }));

        endpoints.MapGet("/health/ready", async (ServerReadinessProbe probe, CancellationToken ct) =>
        {
            var result = await probe.CheckAsync(ct);
            return result.Ready ? Results.Ok(result) : Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        endpoints.MapGet("/api/server/info", (IOptions<DentallaServerOptions> options, IFileStorage fileStorage) =>
        {
            var value = options.Value;
            return Results.Ok(new
            {
                product = "Dentalla MIS",
                component = "Server",
                mode = value.Mode,
                clinicInstance = value.ClinicInstanceName,
                lanEnabled = value.EnableLanEndpoints,
                storageRoot = fileStorage.RootPath,
                apiVersion = "0.3"
            });
        });

        endpoints.MapGet("/api/server/database", async (DatabaseStatusReader reader, CancellationToken ct) =>
        {
            var status = await reader.ReadAsync(ct);
            return status.CanConnect
                ? Results.Ok(status)
                : Results.Json(status, statusCode: StatusCodes.Status503ServiceUnavailable);
        });

        return endpoints;
    }
}
