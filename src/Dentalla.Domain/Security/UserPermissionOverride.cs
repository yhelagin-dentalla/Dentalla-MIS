namespace Dentalla.Domain.Security;

public sealed class UserPermissionOverride
{
    public Guid Id { get; private set; }
    public Guid UserAccountId { get; private set; }
    public string PermissionCode { get; private set; } = string.Empty;
    public bool IsAllowed { get; private set; }
    public string ScopeType { get; private set; } = "Clinic";
    public string? ScopeValue { get; private set; }
    public decimal? LimitAmount { get; private set; }
    public DateTimeOffset ValidFromUtc { get; private set; }
    public DateTimeOffset? ValidToUtc { get; private set; }
    public Guid GrantedByUserAccountId { get; private set; }
    public string? Reason { get; private set; }

    private UserPermissionOverride() { }

    public UserPermissionOverride(
        Guid id,
        Guid userAccountId,
        string permissionCode,
        bool isAllowed,
        string scopeType,
        string? scopeValue,
        decimal? limitAmount,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc,
        Guid grantedByUserAccountId,
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(permissionCode)) throw new ArgumentException("Permission code is required.", nameof(permissionCode));
        if (validToUtc is not null && validToUtc <= validFromUtc) throw new ArgumentException("ValidTo must be later than ValidFrom.", nameof(validToUtc));
        if (limitAmount is < 0) throw new ArgumentOutOfRangeException(nameof(limitAmount));

        Id = id;
        UserAccountId = userAccountId;
        PermissionCode = permissionCode.Trim();
        IsAllowed = isAllowed;
        ScopeType = string.IsNullOrWhiteSpace(scopeType) ? "Clinic" : scopeType.Trim();
        ScopeValue = string.IsNullOrWhiteSpace(scopeValue) ? null : scopeValue.Trim();
        LimitAmount = limitAmount;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        GrantedByUserAccountId = grantedByUserAccountId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}
