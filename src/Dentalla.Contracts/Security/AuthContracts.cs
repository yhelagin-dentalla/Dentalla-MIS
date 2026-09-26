namespace Dentalla.Contracts.Security;

public sealed record BootstrapStatusDto(bool Required);

public sealed record BootstrapDirectorRequest(
    string UserName,
    string Password,
    string DisplayName,
    string? ClientName = null);

public sealed record LoginRequest(
    string UserName,
    string Password,
    string? ClientName = null);

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    CurrentUserDto User);

public sealed record CurrentUserDto(
    Guid UserAccountId,
    Guid StaffProfileId,
    string UserName,
    string DisplayName,
    IReadOnlyList<string> Roles);

public sealed record EffectivePermissionDto(
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
