namespace Dentalla.Domain.Security;

public sealed class UserCredential
{
    public Guid UserAccountId { get; private set; }
    public string PasswordHashBase64 { get; private set; } = string.Empty;
    public string PasswordSaltBase64 { get; private set; } = string.Empty;
    public int PasswordIterations { get; private set; }
    public DateTimeOffset PasswordChangedAtUtc { get; private set; }

    private UserCredential() { }

    public UserCredential(
        Guid userAccountId,
        string passwordHashBase64,
        string passwordSaltBase64,
        int passwordIterations,
        DateTimeOffset passwordChangedAtUtc)
    {
        if (userAccountId == Guid.Empty) throw new ArgumentException("User account id is required.", nameof(userAccountId));
        if (string.IsNullOrWhiteSpace(passwordHashBase64)) throw new ArgumentException("Password hash is required.", nameof(passwordHashBase64));
        if (string.IsNullOrWhiteSpace(passwordSaltBase64)) throw new ArgumentException("Password salt is required.", nameof(passwordSaltBase64));
        if (passwordIterations < 100_000) throw new ArgumentOutOfRangeException(nameof(passwordIterations));

        UserAccountId = userAccountId;
        PasswordHashBase64 = passwordHashBase64;
        PasswordSaltBase64 = passwordSaltBase64;
        PasswordIterations = passwordIterations;
        PasswordChangedAtUtc = passwordChangedAtUtc;
    }

    public void ChangePassword(string hashBase64, string saltBase64, int iterations, DateTimeOffset changedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(hashBase64)) throw new ArgumentException("Password hash is required.", nameof(hashBase64));
        if (string.IsNullOrWhiteSpace(saltBase64)) throw new ArgumentException("Password salt is required.", nameof(saltBase64));
        if (iterations < 100_000) throw new ArgumentOutOfRangeException(nameof(iterations));

        PasswordHashBase64 = hashBase64;
        PasswordSaltBase64 = saltBase64;
        PasswordIterations = iterations;
        PasswordChangedAtUtc = changedAtUtc;
    }
}
