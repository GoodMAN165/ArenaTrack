using ArenaTrack.Data;
using ArenaTrack.Models;

namespace ArenaTrack.Services;

public static class LogService
{
    private const int MaxRecords = 500;
    public static void Write(string source, string level, string message)
    {
        try
        {
            using var db = new AppDbContext();
            db.SyncLogs.Add(new SyncLog
            {
                TimeUtc = DateTime.UtcNow,
                Source = source,
                Level = level,
                Message = message.Length > 500 ? message[..500] : message
            });
            db.SaveChanges();

            int count = db.SyncLogs.Count();
            if (count > MaxRecords)
            {
                var old = db.SyncLogs.OrderBy(l => l.Id).Take(count - MaxRecords).ToList();
                db.SyncLogs.RemoveRange(old);
                db.SaveChanges();
            }
        }
        catch { }
    }

    public static List<SyncLog> GetRecent(int count = 200)
    {
        using var db = new AppDbContext();
        return db.SyncLogs.OrderByDescending(l => l.Id).Take(count).ToList();
    }

    public static void Clear()
    {
        using var db = new AppDbContext();
        db.SyncLogs.RemoveRange(db.SyncLogs);
        db.SaveChanges();
    }
}