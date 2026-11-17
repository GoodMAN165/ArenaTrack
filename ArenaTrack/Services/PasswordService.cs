using System.Security.Cryptography;

namespace ArenaTrack.Services;

public static class PasswordService
{
    private const int Iterations = 100_000;
    public static bool IsUnlocked { get; set; }

    public static bool HasPassword => !string.IsNullOrEmpty(AppSettings.PasswordHash);

    public static void SetPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        AppSettings.PasswordSalt = Convert.ToBase64String(salt);
        AppSettings.PasswordHash = Convert.ToBase64String(Hash(password, salt));
        AppSettings.Save();
    }

    public static bool Verify(string password)
    {
        if (!HasPassword) return false;
        var salt = Convert.FromBase64String(AppSettings.PasswordSalt!);
        var expected = Convert.FromBase64String(AppSettings.PasswordHash!);
        return CryptographicOperations.FixedTimeEquals(Hash(password, salt), expected);
    }

    private static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
}