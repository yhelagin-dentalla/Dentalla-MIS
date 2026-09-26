using System.Net;
using System.Security.Claims;
using Dentalla.Application.Security;
using Dentalla.Contracts.Security;

namespace Dentalla.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapGet("/bootstrap/status", async (ILocalAuthenticationService auth, CancellationToken ct) =>
            Results.Ok(new BootstrapStatusDto(await auth.IsBootstrapRequiredAsync(ct))));

        group.MapPost("/bootstrap/director", async (
            HttpContext context,
            BootstrapDirectorRequest request,
            ILocalAuthenticationService auth,
            CancellationToken ct) =>
        {
            if (!IsLoopback(context.Connection.RemoteIpAddress))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            try
            {
                var issued = await auth.BootstrapDirectorAsync(
                    request.UserName,
                    request.Password,
                    request.DisplayName,
                    request.ClientName,
                    context.Connection.RemoteIpAddress?.ToString(),
                    context.TraceIdentifier,
                    ct);

                return Results.Ok(ToResponse(issued));
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/login", async (
            HttpContext context,
            LoginRequest request,
            ILocalAuthenticationService auth,
            CancellationToken ct) =>
        {
            var issued = await auth.LoginAsync(
                request.UserName,
                request.Password,
                request.ClientName,
                context.Connection.RemoteIpAddress?.ToString(),
                context.TraceIdentifier,
                ct);

            return issued is null
                ? Results.Unauthorized()
                : Results.Ok(ToResponse(issued));
        });

        var protectedGroup = group.MapGroup(string.Empty).RequireAuthorization();

        protectedGroup.MapGet("/me", (ClaimsPrincipal user) =>
        {
            if (!TryReadIdentity(user, out var current))
                return Results.Unauthorized();

            return Results.Ok(current);
        });

        protectedGroup.MapGet("/me/permissions", async (
            ClaimsPrincipal user,
            IEffectivePermissionService permissions,
            CancellationToken ct) =>
        {
            if (!TryGetGuidClaim(user, ClaimTypes.NameIdentifier, out var userId))
                return Results.Unauthorized();

            var resolved = await permissions.ResolveAsync(userId, ct);
            return Results.Ok(resolved.Select(x => new EffectivePermissionDto(
                x.PermissionCode,
                x.Name,
                x.Area,
                x.IsAllowed,
                x.Source,
                x.ScopeType,
                x.ScopeValue,
                x.LimitAmount,
                x.IsSensitive,
                x.IsClinicalPrivilegeBound)));
        });

        protectedGroup.MapPost("/logout", async (
            HttpContext context,
            ClaimsPrincipal user,
            ILocalAuthenticationService auth,
            CancellationToken ct) =>
        {
            if (!TryGetGuidClaim(user, ClaimTypes.NameIdentifier, out var userId)
                || !TryGetGuidClaim(user, "dentalla_session_id", out var sessionId))
                return Results.Unauthorized();

            await auth.RevokeSessionAsync(
                sessionId,
                userId,
                context.Connection.RemoteIpAddress?.ToString(),
                context.TraceIdentifier,
                ct);

            return Results.NoContent();
        });

        return endpoints;
    }

    private static AuthTokenResponse ToResponse(IssuedSession issued)
        => new(
            issued.AccessToken,
            issued.ExpiresAtUtc,
            new CurrentUserDto(
                issued.Session.UserAccountId,
                issued.Session.StaffProfileId,
                issued.Session.UserName,
                issued.Session.DisplayName,
                issued.Session.Roles));

    private static bool TryReadIdentity(ClaimsPrincipal principal, out CurrentUserDto dto)
    {
        dto = default!;
        if (!TryGetGuidClaim(principal, ClaimTypes.NameIdentifier, out var userId)
            || !TryGetGuidClaim(principal, "dentalla_staff_profile_id", out var staffId))
            return false;

        var userName = principal.Identity?.Name;
        var displayName = principal.FindFirst("dentalla_display_name")?.Value;
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(displayName))
            return false;

        var roles = principal.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray();
        dto = new CurrentUserDto(userId, staffId, userName, displayName, roles);
        return true;
    }

    private static bool TryGetGuidClaim(ClaimsPrincipal principal, string claimType, out Guid value)
        => Guid.TryParse(principal.FindFirst(claimType)?.Value, out value);

    private static bool IsLoopback(IPAddress? address)
        => address is not null && (IPAddress.IsLoopback(address) || address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4()));
}
