namespace Dentalla.Application.Security;

public sealed record AuthenticatedSession(
    Guid SessionId,
    Guid UserAccountId,
    Guid StaffProfileId,
    string UserName,
    string DisplayName,
    IReadOnlyList<string> Roles,
    DateTimeOffset ExpiresAtUtc);

public sealed record IssuedSession(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    AuthenticatedSession Session);

public sealed record EffectivePermissionResult(
    string PermissionCode,
    string Name,
    string Area,
    bool IsAllowed,
    string Source,
    string ScopeType,
    string? ScopeValue,
    decimal? LimitAmount,
    bool IsSensitive,
    bool IsClinicalPrivilegeBound);
