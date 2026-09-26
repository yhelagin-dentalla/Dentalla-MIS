using System.Security.Cryptography;

namespace Dentalla.Infrastructure.Security;

public sealed class PasswordHasher
{
    public const int DefaultIterations = 310_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public PasswordHash Create(string password)
    {
        ValidatePassword(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return new PasswordHash(
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt),
            DefaultIterations);
    }

    public bool Verify(string password, string hashBase64, string saltBase64, int iterations)
    {
        if (string.IsNullOrEmpty(password) || iterations < 100_000)
            return false;

        try
        {
            var expected = Convert.FromBase64String(hashBase64);
            var salt = Convert.FromBase64String(saltBase64);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expected.Length);

            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 10)
            throw new ArgumentException("Password must contain at least 10 characters.", nameof(password));
        if (password.Length > 200)
            throw new ArgumentException("Password is too long.", nameof(password));
    }
}

public sealed record PasswordHash(string HashBase64, string SaltBase64, int Iterations);
