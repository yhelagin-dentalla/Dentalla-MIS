namespace Dentalla.Domain.Security;

public sealed class RolePermission
{
    public string RoleCode { get; private set; } = string.Empty;
    public string PermissionCode { get; private set; } = string.Empty;
    public bool IsAllowed { get; private set; }

    private RolePermission() { }

    public RolePermission(string roleCode, string permissionCode, bool isAllowed = true)
    {
        RoleCode = roleCode;
        PermissionCode = permissionCode;
        IsAllowed = isAllowed;
    }
}
