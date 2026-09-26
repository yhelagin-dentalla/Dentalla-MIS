namespace Dentalla.Domain.Security;

public sealed class DelegationGrant
{
    public Guid Id { get; private set; }
    public Guid GrantedByUserAccountId { get; private set; }
    public Guid GrantedToUserAccountId { get; private set; }
    public string PermissionCode { get; private set; } = string.Empty;
    public string ScopeType { get; private set; } = "Clinic";
    public string? ScopeValue { get; private set; }
    public decimal? LimitAmount { get; private set; }
    public DateTimeOffset ValidFromUtc { get; private set; }
    public DateTimeOffset ValidToUtc { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? RevokedByUserAccountId { get; private set; }

    private DelegationGrant() { }

    public DelegationGrant(
        Guid id,
        Guid grantedByUserAccountId,
        Guid grantedToUserAccountId,
        string permissionCode,
        string scopeType,
        string? scopeValue,
        decimal? limitAmount,
        DateTimeOffset validFromUtc,
        DateTimeOffset validToUtc,
        string reason)
    {
        if (grantedByUserAccountId == grantedToUserAccountId) throw new ArgumentException("Self delegation is not allowed.");
        if (string.IsNullOrWhiteSpace(permissionCode)) throw new ArgumentException("Permission code is required.", nameof(permissionCode));
        if (validToUtc <= validFromUtc) throw new ArgumentException("Delegation must have a finite positive validity interval.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Delegation reason is required.", nameof(reason));

        Id = id;
        GrantedByUserAccountId = grantedByUserAccountId;
        GrantedToUserAccountId = grantedToUserAccountId;
        PermissionCode = permissionCode.Trim();
        ScopeType = string.IsNullOrWhiteSpace(scopeType) ? "Clinic" : scopeType.Trim();
        ScopeValue = string.IsNullOrWhiteSpace(scopeValue) ? null : scopeValue.Trim();
        LimitAmount = limitAmount;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        Reason = reason.Trim();
    }

    public void Revoke(Guid revokedByUserAccountId, DateTimeOffset revokedAtUtc)
    {
        if (RevokedAtUtc is not null) return;
        RevokedByUserAccountId = revokedByUserAccountId;
        RevokedAtUtc = revokedAtUtc;
    }
}
