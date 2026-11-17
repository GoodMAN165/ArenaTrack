using ArenaTrack.Data;
using ArenaTrack.Models;
using Microsoft.Data.Sqlite;
using MatchModel = ArenaTrack.Models.Match;

namespace ArenaTrack.Tests;

/// <summary>Временная база SQLite: создаётся в конструкторе, удаляется в Dispose.</summary>
public sealed class TestDb : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"arenatrack-test-{Guid.NewGuid():N}.db");

    public TestDb()
    {
        AppDbContext.DbPathOverride = _path;
        using var db = new AppDbContext();
        db.Database.EnsureCreated();
    }

    public int AddTournament(string game, string name = "Test Cup")
    {
        using var db = new AppDbContext();
        var t = new Tournament
        {
            Name = name,
            Game = game,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow
        };
        db.Tournaments.Add(t);
        db.SaveChanges();
        return t.Id;
    }
    public int AddMatch(int tournamentId, string game, string team1, string team2,
                        string status, double hoursAgo,
                        int? score1 = null, int? score2 = null, string? result = null)
    {
        using var db = new AppDbContext();
        var m = new MatchModel
        {
            TournamentId = tournamentId,
            Game = game,
            Team1 = team1,
            Team2 = team2,
            Status = status,
            StartTimeUtc = DateTime.UtcNow.AddHours(-hoursAgo),
            Score1 = score1,
            Score2 = score2,
            ResultText = result
        };
        db.Matches.Add(m);
        db.SaveChanges();
        return m.Id;
    }

    public void Dispose()
    {
        AppDbContext.DbPathOverride = null;
        SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _path, _path + "-wal", _path + "-shm" })
            try { File.Delete(f); } catch { }
    }
}