namespace Dentalla.Domain.Security;

public sealed class AuthSession
{
    public Guid Id { get; private set; }
    public Guid UserAccountId { get; private set; }
    public string TokenHashHex { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset LastSeenAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ClientName { get; private set; }
    public string? CreatedFromIp { get; private set; }

    private AuthSession() { }

    public AuthSession(
        Guid id,
        Guid userAccountId,
        string tokenHashHex,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string? clientName,
        string? createdFromIp)
    {
        if (id == Guid.Empty) throw new ArgumentException("Session id is required.", nameof(id));
        if (userAccountId == Guid.Empty) throw new ArgumentException("User account id is required.", nameof(userAccountId));
        if (string.IsNullOrWhiteSpace(tokenHashHex)) throw new ArgumentException("Session token hash is required.", nameof(tokenHashHex));
        if (expiresAtUtc <= createdAtUtc) throw new ArgumentException("Session expiration must be later than creation time.", nameof(expiresAtUtc));

        Id = id;
        UserAccountId = userAccountId;
        TokenHashHex = tokenHashHex;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        LastSeenAtUtc = createdAtUtc;
        ClientName = string.IsNullOrWhiteSpace(clientName) ? null : clientName.Trim();
        CreatedFromIp = string.IsNullOrWhiteSpace(createdFromIp) ? null : createdFromIp.Trim();
    }

    public bool IsActiveAt(DateTimeOffset nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    public void Touch(DateTimeOffset atUtc)
    {
        if (atUtc > LastSeenAtUtc)
            LastSeenAtUtc = atUtc;
    }

    public void Revoke(DateTimeOffset atUtc)
    {
        RevokedAtUtc ??= atUtc;
    }
}
