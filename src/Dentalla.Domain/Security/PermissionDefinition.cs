namespace Dentalla.Domain.Security;

public sealed class PermissionDefinition
{
    public string Code { get; private set; } = string.Empty;
    public string Area { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsSensitive { get; private set; }
    public bool IsClinicalPrivilegeBound { get; private set; }

    private PermissionDefinition() { }

    public PermissionDefinition(string code, string area, string name, bool isSensitive = false, bool isClinicalPrivilegeBound = false)
    {
        Code = code;
        Area = area;
        Name = name;
        IsSensitive = isSensitive;
        IsClinicalPrivilegeBound = isClinicalPrivilegeBound;
    }
}
