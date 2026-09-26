namespace Dentalla.Application.Security;

public interface ILocalAuthenticationService
{
    Task<bool> IsBootstrapRequiredAsync(CancellationToken cancellationToken = default);

    Task<IssuedSession> BootstrapDirectorAsync(
        string userName,
        string password,
        string displayName,
        string? clientName,
        string? clientIp,
        string? traceId,
        CancellationToken cancellationToken = default);

    Task<IssuedSession?> LoginAsync(
        string userName,
        string password,
        string? clientName,
        string? clientIp,
        string? traceId,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedSession?> AuthenticateAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(
        Guid sessionId,
        Guid actorUserAccountId,
        string? clientIp,
        string? traceId,
        CancellationToken cancellationToken = default);
}
