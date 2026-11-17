using System.Windows;
using ArenaTrack.Data;
using ArenaTrack.Services;

namespace ArenaTrack;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated();
        }

        NotificationService.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        NotificationService.Stop();
        base.OnExit(e);
    }
}