using Dentalla.Application.Security;

namespace Dentalla.Api.Endpoints;

public static class LoginDirectoryEndpoints
{
    public static IEndpointRouteBuilder MapLoginDirectoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/auth/dev-login-directory", async (
            ILoginDirectoryReader reader,
            CancellationToken ct) => Results.Ok(await reader.ReadAsync(ct)));

        return endpoints;
    }
}
