using System.Security.Cryptography;
using System.Text;

namespace NurSapat.Infrastructure.Sync.Auth;

/// <summary>
/// Salted SHA-256 password hasher for local offline auth cache.
/// Stored format: "{base64Salt}:{base64Hash}".
/// </summary>
public static class OfflinePasswordHasher
{
    private const int SaltSize = 16;

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Hash(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Hash(password, salt);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Hash(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var payload = new byte[salt.Length + passwordBytes.Length];
        Buffer.BlockCopy(salt, 0, payload, 0, salt.Length);
        Buffer.BlockCopy(passwordBytes, 0, payload, salt.Length, passwordBytes.Length);
        return SHA256.HashData(payload);
    }
}
