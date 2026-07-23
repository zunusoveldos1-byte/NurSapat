using System.Security.Cryptography;
using System.Text;

namespace NurSapat.App.Services.Utilities;

internal static class PackageEncryptionHelper
{
    public static byte[] Encrypt(byte[] plainBytes, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return plainBytes;
        }

        var salt = RandomNumberGenerator.GetBytes(16);
        using var aes = Aes.Create();
        aes.Key = DeriveKey(password, salt);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        using var output = new MemoryStream();
        output.Write(salt);
        output.Write(aes.IV);
        output.Write(cipher);
        return output.ToArray();
    }

    public static byte[] Decrypt(byte[] encryptedBytes, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return encryptedBytes;
        }

        if (encryptedBytes.Length < 32)
        {
            throw new InvalidOperationException("Некорректный зашифрованный пакет.");
        }

        var salt = encryptedBytes[..16];
        var iv = encryptedBytes[16..32];
        var cipher = encryptedBytes[32..];

        using var aes = Aes.Create();
        aes.Key = DeriveKey(password, salt);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
    }

    private static byte[] DeriveKey(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);
}
