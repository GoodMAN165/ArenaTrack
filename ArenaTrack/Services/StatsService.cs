using Microsoft.EntityFrameworkCore;
using ArenaTrack.Data;
using ArenaTrack.Models;

namespace ArenaTrack.Services;

public record Stat(string Label, string Value);

public record MatchRow(string Time, string Opponent, string Score, string Result, string Tournament);
public record RosterRow(string Nickname, string FullName, string Country);
public class ProfileData
{
    public List<Stat> Stats { get; set; } = new();
    public List<MatchRow> Rows { get; set; } = new();
    public List<RosterRow> Roster { get; set; } = new();
    public string Subtitle { get; set; } = "";
}

public static class StatsService
{
    private static string Rating(int? r) => r is > 0 ? r.ToString()! : "—";
    public static List<string> GetNames(string game)
    {
        using var db = new AppDbContext();
        var pairs = db.Matches.Where(m => m.Game == game)
                              .Select(m => new { m.Team1, m.Team2 }).ToList();

        return pairs.SelectMany(p => new[] { p.Team1, p.Team2 })
                    .Where(n => !string.IsNullOrWhiteSpace(n) && n != "?")
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
    }

    public static ProfileData GetProfile(string name, string game)
    {

        using var db = new AppDbContext();
        var matches = db.Matches.Include(m => m.Tournament)
            .Where(m => m.Game == game && (m.Team1 == name || m.Team2 == name))
            .AsEnumerable()
            .OrderByDescending(m => m.StartTimeUtc)
            .ToList();

        var outcomes = new List<int>();
        var rows = new List<MatchRow>();

        foreach (var m in matches.Take(30))
        {
            bool isFirst = m.Team1 == name;
            int? o = Outcome(m);
            if (o != null && !isFirst) o = -o;

            string result = o switch
            {
                1 => "Победа",
                -1 => "Поражение",
                0 => "Ничья",
                _ => m.StatusText
            };

            rows.Add(new MatchRow(
                m.LocalTime,
                isFirst ? m.Team2 : m.Team1,
                ScoreFor(m, isFirst),
                result,
                m.TournamentName));
        }

        foreach (var m in matches)
        {
            int? o = Outcome(m);
            if (o == null) continue;
            outcomes.Add(m.Team1 == name ? o.Value : -o.Value);
        }

        int played = outcomes.Count;
        int wins = outcomes.Count(x => x == 1);
        int losses = outcomes.Count(x => x == -1);
        int draws = outcomes.Count(x => x == 0);

        var stats = new List<Stat>
        {
            new("Матчей", played.ToString()),
            new("Побед", wins.ToString()),
            new("Поражений", losses.ToString())
        };
        if (game == "Chess") stats.Add(new Stat("Ничьих", draws.ToString()));

        stats.Add(new Stat("Винрейт", played == 0 ? "—" : $"{wins * 100.0 / played:0}%"));
        stats.Add(new Stat("Серия", Streak(outcomes)));

        string subtitle = game;

        if (game == "Chess")
        {
            var info = db.PlayerInfos.FirstOrDefault(p => p.Name == name);
            if (info != null)
            {
                stats.Add(new Stat("Классика", Rating(info.Standard)));
                stats.Add(new Stat("Рапид", Rating(info.Rapid)));
                stats.Add(new Stat("Блиц", Rating(info.Blitz)));
                stats.Add(new Stat("Федерация", info.Federation ?? "—"));

                if (!string.IsNullOrEmpty(info.Title))
                    subtitle = $"Chess · {info.Title}";
            }
        }

        var roster = new List<RosterRow>();
        if (game == "Valorant")
        {
            var team = db.Teams.FirstOrDefault(t => t.Game == game && t.Name == name);
            if (team != null)
                roster = db.RosterPlayers.Where(p => p.TeamId == team.Id)
                           .OrderBy(p => p.Nickname).ToList()
                           .Select(p => new RosterRow(p.Nickname, p.FullName, p.Country))
                           .ToList();
        }

        return new ProfileData { Stats = stats, Rows = rows, Subtitle = subtitle, Roster = roster };
    }

    private static int? Outcome(Match m)
    {
        if (m.Status != "Finished") return null;

        if (m.ResultText == "1 : 0") return 1;
        if (m.ResultText == "0 : 1") return -1;
        if (m.ResultText == "½ : ½") return 0;

        if (m.Score1 is int a && m.Score2 is int b)
            return a > b ? 1 : a < b ? -1 : 0;

        return null;
    }

    private static string ScoreFor(Match m, bool isFirst)
    {
        var s = m.ScoreText;
        if (isFirst || !s.Contains(" : ")) return s;
        var p = s.Split(" : ");
        return $"{p[1]} : {p[0]}";
    }
    
    
    private static string Streak(List<int> outcomes)
    {
        if (outcomes.Count == 0) return "—";

        int first = outcomes[0];
        int n = outcomes.TakeWhile(x => x == first).Count();
        string word = first == 1 ? "Победы" : first == -1 ? "Поражения" : "Ничьи";
        return $"{word}: {n}";
    }
}