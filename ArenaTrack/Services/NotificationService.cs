using System.Drawing;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using ArenaTrack.Data;

namespace ArenaTrack.Services;

public static class NotificationService
{
    private static Forms.NotifyIcon? _icon;
    private static DispatcherTimer? _timer;
    private static readonly HashSet<int> _notified = new();

    public static void Start()
    {
        _icon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ArenaTrack",
            Visible = true
        };

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) => Check();
        _timer.Start();
        Check();
    }

    public static void Stop()
    {
        _timer?.Stop();
        if (_icon != null)
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }

    public static void ShowTest() =>
        Show("ArenaTrack", "Тестовое уведомление: всё работает.");

    private static void Show(string title, string text)
    {
        _icon?.ShowBalloonTip(8000, title, text, Forms.ToolTipIcon.Info);
    }

    private static void Check()
    {
        if (!AppSettings.NotificationsEnabled) return;

        try
        {
            using var db = new AppDbContext();
            var ids = FavoriteService.GetIds();
            var now = DateTime.UtcNow;
            var limit = now.AddMinutes(AppSettings.NotifyMinutes);

            var soon = db.Matches
                .Where(m => ids.Contains(m.Id) && m.Status == "Upcoming")
                .AsEnumerable()
                .Where(m => m.StartTimeUtc > now && m.StartTimeUtc <= limit
                            && !_notified.Contains(m.Id))
                .ToList();

            foreach (var m in soon)
            {
                int minutes = (int)Math.Ceiling((m.StartTimeUtc - now).TotalMinutes);
                Show($"{m.Team1} — {m.Team2}", $"Начало через {minutes} мин. ({m.Game})");
                _notified.Add(m.Id);
            }
        }
        catch
        {
        }
    }
}