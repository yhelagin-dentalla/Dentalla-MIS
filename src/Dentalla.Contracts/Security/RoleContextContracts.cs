namespace Dentalla.Contracts.Security;

public sealed record DevRoleContextSwitchRequest(
    Guid StaffProfileId,
    string FromRoleCode,
    string ToRoleCode);

public sealed record RoleContextSwitchResponse(
    bool Allowed,
    string ActiveRoleCode,
    string Message);
