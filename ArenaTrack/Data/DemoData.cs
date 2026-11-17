using ArenaTrack.Models;
using MatchModel = ArenaTrack.Models.Match;

namespace ArenaTrack.Data;

public static class DemoData
{
    private const string Prefix = "demo:";

    public static bool IsLoaded()
    {
        using var db = new AppDbContext();
        return db.Matches.Any(m => m.ExternalId != null && m.ExternalId.StartsWith(Prefix));
    }

    public static void Load()
    {
        using var db = new AppDbContext();
        if (db.Matches.Any(m => m.ExternalId != null && m.ExternalId.StartsWith(Prefix))) return;

        var now = DateTime.UtcNow;

        var vct = new Tournament
        {
            ExternalId = Prefix + "vct",
            Name = "VCT Champions (демо)",
            Game = "Valorant",
            PrizePool = "$2 250 000",
            StartDate = now.AddDays(-3),
            EndDate = now.AddDays(10)
        };
        var cand = new Tournament
        {
            ExternalId = Prefix + "cand",
            Name = "Candidates Tournament (демо)",
            Game = "Chess",
            PrizePool = null,
            StartDate = now.AddDays(-2),
            EndDate = now.AddDays(14)
        };
        db.Tournaments.AddRange(vct, cand);
        db.SaveChanges();

        int n = 0;
        MatchModel M(Tournament t, string game, string a, string b, string status,
                     DateTime time, int? s1 = null, int? s2 = null, string? result = null) => new()
                     {
                         ExternalId = Prefix + (++n),
                         Game = game,
                         TournamentId = t.Id,
                         Team1 = a,
                         Team2 = b,
                         Status = status,
                         StartTimeUtc = time,
                         Score1 = s1,
                         Score2 = s2,
                         ResultText = result
                     };

        db.Matches.AddRange(
            M(vct, "Valorant", "Sentinels", "Fnatic", "Finished", now.AddHours(-5), 2, 1),
            M(vct, "Valorant", "Sentinels", "LOUD", "Finished", now.AddDays(-1), 2, 0),
            M(vct, "Valorant", "Fnatic", "Team Liquid", "Finished", now.AddDays(-2), 0, 2),
            M(vct, "Valorant", "Team Liquid", "Paper Rex", "Live", now.AddMinutes(-40), 1, 1),
            M(vct, "Valorant", "LOUD", "DRX", "Upcoming", now.AddMinutes(20)),   // для проверки уведомления
            M(vct, "Valorant", "Sentinels", "Paper Rex", "Upcoming", now.AddDays(1)),

            M(cand, "Chess", "Hikaru Nakamura", "Gukesh D", "Finished", now.AddHours(-8), result: "1 : 0"),
            M(cand, "Chess", "Fabiano Caruana", "Hikaru Nakamura", "Finished", now.AddDays(-1), result: "½ : ½"),
            M(cand, "Chess", "Gukesh D", "Fabiano Caruana", "Finished", now.AddDays(-2), result: "0 : 1"),
            M(cand, "Chess", "Gukesh D", "Hikaru Nakamura", "Upcoming", now.AddHours(6)),
            M(cand, "Chess", "Fabiano Caruana", "Praggnanandhaa R", "Upcoming", now.AddDays(1))
        );
        db.SaveChanges();
    }

    public static void Remove()
    {
        using var db = new AppDbContext();

        var ids = db.Matches
            .Where(m => m.ExternalId != null && m.ExternalId.StartsWith(Prefix))
            .Select(m => m.Id).ToList();

        db.Favorites.RemoveRange(db.Favorites.Where(f => ids.Contains(f.MatchId)));
        db.Matches.RemoveRange(db.Matches.Where(m => ids.Contains(m.Id)));
        db.Tournaments.RemoveRange(db.Tournaments
            .Where(t => t.ExternalId != null && t.ExternalId.StartsWith(Prefix)));
        db.SaveChanges();
    }
}