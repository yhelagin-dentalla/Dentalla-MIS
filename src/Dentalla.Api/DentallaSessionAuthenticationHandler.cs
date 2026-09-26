using System.Security.Claims;
using System.Text.Encodings.Web;
using Dentalla.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Dentalla.Api;

public static class DentallaSessionDefaults
{
    public const string Scheme = "DentallaSession";
}

public sealed class DentallaSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ILocalAuthenticationService authenticationService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
            return AuthenticateResult.NoResult();

        const string bearerPrefix = "Bearer ";
        if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = authorization[bearerPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return AuthenticateResult.Fail("Missing bearer token.");

        var session = await authenticationService.AuthenticateAsync(token, Context.RequestAborted);
        if (session is null)
            return AuthenticateResult.Fail("Invalid or expired Dentalla session.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserAccountId.ToString()),
            new(ClaimTypes.Name, session.UserName),
            new("dentalla_display_name", session.DisplayName),
            new("dentalla_staff_profile_id", session.StaffProfileId.ToString()),
            new("dentalla_session_id", session.SessionId.ToString())
        };

        foreach (var role in session.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
