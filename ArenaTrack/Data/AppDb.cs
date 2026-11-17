using ArenaTrack.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ArenaTrack.Data;

public class AppDbContext : DbContext
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<PlayerInfo> PlayerInfos => Set<PlayerInfo>();
    public DbSet<RosterPlayer> RosterPlayers => Set<RosterPlayer>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public static string? DbPathOverride { get; set; }
    public static string DbPath
    {
        get
        {
            if (DbPathOverride != null) return DbPathOverride;

            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArenaTrack");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "arenatrack.db");
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Favorite>().HasIndex(f => f.MatchId).IsUnique();
        modelBuilder.Entity<PlayerInfo>().HasIndex(p => p.Name).IsUnique();
    }
}