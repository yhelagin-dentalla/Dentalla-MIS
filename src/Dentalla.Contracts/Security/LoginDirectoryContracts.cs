namespace Dentalla.Contracts.Security;

public sealed record LoginDirectoryDto(
    bool SourceReady,
    string Source,
    string? Message,
    IReadOnlyList<LoginRoleDto> Roles,
    IReadOnlyList<LoginEmployeeDto> Employees);

public sealed record LoginRoleDto(
    string Code,
    string Name,
    string Description);

public sealed record LoginEmployeeDto(
    string Id,
    string DisplayName,
    IReadOnlyList<string> RoleCodes);
