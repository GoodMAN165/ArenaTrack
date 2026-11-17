using System.IO;
using System.Text.Json;

namespace ArenaTrack.Services;

public static class AppSettings
{
    private class Data
    {
        public bool NotificationsEnabled { get; set; } = true;
        public int NotifyMinutes { get; set; } = 15;
        public DateTime? LastSyncUtc { get; set; }
        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }
        public bool ChessEnabled { get; set; } = true;
        public bool ValorantEnabled { get; set; } = true;
    }

    public static string? PathOverride { get; set; }

    private static string FilePath => PathOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ArenaTrack", "settings.json");

    private static Data _data = Load();

    public static bool NotificationsEnabled
    {
        get => _data.NotificationsEnabled;
        set => _data.NotificationsEnabled = value;
    }

    public static int NotifyMinutes
    {
        get => _data.NotifyMinutes;
        set => _data.NotifyMinutes = value;
    }

    public static DateTime? LastSyncUtc
    {
        get => _data.LastSyncUtc;
        set => _data.LastSyncUtc = value;
    }

    public static string? PasswordHash
    {
        get => _data.PasswordHash;
        set => _data.PasswordHash = value;
    }

    public static string? PasswordSalt
    {
        get => _data.PasswordSalt;
        set => _data.PasswordSalt = value;
    }

    public static bool ChessEnabled
    {
        get => _data.ChessEnabled;
        set => _data.ChessEnabled = value;
    }

    public static bool ValorantEnabled
    {
        get => _data.ValorantEnabled;
        set => _data.ValorantEnabled = value;
    }

    private static Data Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Data>(File.ReadAllText(FilePath)) ?? new Data();
        }
        catch { /* повреждённый файл: значения по умолчанию */ }
        return new Data();
    }

    public static void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(_data));
    }
}