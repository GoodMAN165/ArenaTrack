using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ArenaTrack.Services;
public static class ApiKeyStore
{
    private static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArenaTrack");

    private static string DatPath => Path.Combine(Folder, "pandascore.dat");
    private static string LegacyPath => Path.Combine(Folder, "pandascore.key");

    public static bool Exists => File.Exists(DatPath) || File.Exists(LegacyPath);

    public static void Save(string key)
    {
        Directory.CreateDirectory(Folder);
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(DatPath, encrypted);

        if (File.Exists(LegacyPath)) File.Delete(LegacyPath);
    }

    public static string? Get()
    {
        try
        {
            if (File.Exists(DatPath))
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    File.ReadAllBytes(DatPath), null, DataProtectionScope.CurrentUser));
            if (File.Exists(LegacyPath))
            {
                var plain = File.ReadAllText(LegacyPath).Trim();
                if (plain.Length > 0)
                {
                    Save(plain);
                    return plain;
                }
            }
        }
        catch { }
        return null;
    }
}