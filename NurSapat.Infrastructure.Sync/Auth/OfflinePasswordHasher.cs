using System.Security.Cryptography;
using System.Text;

namespace NurSapat.Infrastructure.Sync.Auth;

/// <summary>
/// PBKDF2-SHA256 password hasher for local-only configuration profiles.
/// The primary login path uses the DPAPI-protected session cache.
/// </summary>
public static class OfflinePasswordHasher
{
    private const int SaltSize = 16;
    private const int Iterations = 210_000;
    private const int HashSize = 32;

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Hash(password, salt);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');
        if (parts.Length != 4 ||
            parts[0] != "pbkdf2-sha256" ||
            !int.TryParse(parts[1], out var iterations) ||
            iterations < 100_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Hash(password, salt, iterations, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Hash(string password, byte[] salt) =>
        Hash(password, salt, Iterations, HashSize);

    private static byte[] Hash(string password, byte[] salt, int iterations, int size) =>
        Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            size);
}
