using System.Net.NetworkInformation;

namespace ArenaTrack.Services;

public static class SyncStatus
{
    public static event Action? Changed;

    public static bool IsOnline => NetworkInterface.GetIsNetworkAvailable();

    private static bool IsStale =>
        AppSettings.LastSyncUtc == null ||
        DateTime.UtcNow - AppSettings.LastSyncUtc > TimeSpan.FromHours(24);
    public static bool NeedsAttention => !IsOnline || IsStale;

    public static void MarkSuccess()
    {
        AppSettings.LastSyncUtc = DateTime.UtcNow;
        AppSettings.Save();
        Changed?.Invoke();
    }

    public static void Refresh() => Changed?.Invoke();

    public static string GetText()
    {
        var last = AppSettings.LastSyncUtc;
        var text = last == null
            ? "Последняя синхронизация: нет данных"
            : $"Последняя синхронизация: {last.Value.ToLocalTime():dd.MM.yyyy HH:mm}";

        if (!IsOnline) return "Офлайн-режим · " + text;
        if (last != null && IsStale) return text + " · данные могут быть устаревшими";
        return text;
    }
}