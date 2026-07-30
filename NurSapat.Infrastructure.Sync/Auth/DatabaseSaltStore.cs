using System.Security.Cryptography;
using System.Text;

namespace NurSapat.Infrastructure.Sync.Auth;

public sealed class DatabaseSaltStore
{
    private static readonly byte[] Entropy =
        SHA256.HashData(Encoding.UTF8.GetBytes("NurSapat/database-salt/v1"));
    private readonly string _directory;

    public DatabaseSaltStore()
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NurSapat",
            "keys");
        Directory.CreateDirectory(_directory);
    }

    public async Task<byte[]> GetOrCreateAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(email);
        if (File.Exists(path))
        {
            var protectedSalt = await File.ReadAllBytesAsync(path, cancellationToken)
                .ConfigureAwait(false);
            return ProtectedData.Unprotect(
                protectedSalt,
                Entropy,
                DataProtectionScope.CurrentUser);
        }

        var salt = RandomNumberGenerator.GetBytes(32);
        var payload = ProtectedData.Protect(
            salt,
            Entropy,
            DataProtectionScope.CurrentUser);
        var temp = path + ".tmp";
        await File.WriteAllBytesAsync(temp, payload, cancellationToken).ConfigureAwait(false);
        File.Move(temp, path, overwrite: true);
        return salt;
    }

    private string GetPath(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return Path.Combine(_directory, name + ".dat");
    }
}
