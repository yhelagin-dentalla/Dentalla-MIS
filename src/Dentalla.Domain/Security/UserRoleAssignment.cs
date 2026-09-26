namespace Dentalla.Domain.Security;

public sealed class UserRoleAssignment
{
    public Guid Id { get; private set; }
    public Guid UserAccountId { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public DateTimeOffset ValidFromUtc { get; private set; }
    public DateTimeOffset? ValidToUtc { get; private set; }
    public Guid GrantedByUserAccountId { get; private set; }
    public string? Reason { get; private set; }

    private UserRoleAssignment() { }

    public UserRoleAssignment(
        Guid id,
        Guid userAccountId,
        string roleCode,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc,
        Guid grantedByUserAccountId,
        string? reason)
    {
        if (!SystemRoleCode.All.Contains(roleCode))
            throw new ArgumentOutOfRangeException(nameof(roleCode), $"Unknown system role '{roleCode}'.");
        if (validToUtc is not null && validToUtc <= validFromUtc)
            throw new ArgumentException("ValidTo must be later than ValidFrom.", nameof(validToUtc));

        Id = id;
        UserAccountId = userAccountId;
        RoleCode = roleCode;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        GrantedByUserAccountId = grantedByUserAccountId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}
