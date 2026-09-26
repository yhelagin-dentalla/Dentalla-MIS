namespace Dentalla.Domain.Security;

public sealed class UserAccount
{
    public Guid Id { get; private set; }
    public Guid StaffProfileId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string NormalizedUserName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? DisabledAtUtc { get; private set; }

    private readonly List<UserRoleAssignment> _roles = [];
    private readonly List<UserPermissionOverride> _permissionOverrides = [];

    public IReadOnlyCollection<UserRoleAssignment> Roles => _roles;
    public IReadOnlyCollection<UserPermissionOverride> PermissionOverrides => _permissionOverrides;

    private UserAccount() { }

    public UserAccount(Guid id, Guid staffProfileId, string userName, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("User id is required.", nameof(id));
        if (staffProfileId == Guid.Empty) throw new ArgumentException("Staff profile id is required.", nameof(staffProfileId));
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("User name is required.", nameof(userName));

        Id = id;
        StaffProfileId = staffProfileId;
        UserName = userName.Trim();
        NormalizedUserName = Normalize(UserName);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public void Disable(DateTimeOffset atUtc)
    {
        IsActive = false;
        DisabledAtUtc = atUtc;
    }

    public void Enable()
    {
        IsActive = true;
        DisabledAtUtc = null;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
