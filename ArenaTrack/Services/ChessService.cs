using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ArenaTrack.Data;
using ArenaTrack.Models;
using MatchModel = ArenaTrack.Models.Match;
using RegexMatch = System.Text.RegularExpressions.Match;

namespace ArenaTrack.Services;
public class ChessService
{
    private static readonly HttpClient Http = CreateClient();


    private static void UpsertPlayer(AppDbContext db, string name, Dictionary<string, string> h, string side)
    {
        var player = db.PlayerInfos.Local.FirstOrDefault(p => p.Name == name)
                     ?? db.PlayerInfos.FirstOrDefault(p => p.Name == name);
        if (player == null)
        {
            player = new PlayerInfo { Name = name };
            db.PlayerInfos.Add(player);
        }

        if (h.TryGetValue(side + "FideId", out var fide) && int.TryParse(fide, out var f) && f > 0)
            player.FideId = f;

        if (h.TryGetValue(side + "Title", out var title) && title.Length > 0)
            player.Title = title;

        if (player.RatingsUpdatedUtc == null &&
            h.TryGetValue(side + "Elo", out var elo) && int.TryParse(elo, out var e) && e > 0)
            player.Standard = e;
    }


    private static async Task SyncRatingsAsync()
    {
        using var db = new AppDbContext();
        var limit = DateTime.UtcNow.AddHours(-24);

        var all = await db.PlayerInfos.Where(p => p.FideId != null).ToListAsync();
        var due = all.Where(p => p.RatingsUpdatedUtc == null || p.RatingsUpdatedUtc < limit)
                     .Take(20)
                     .ToList();

        foreach (var p in due)
        {
            try
            {
                var json = await Http.GetStringAsync($"https://lichess.org/api/fide/player/{p.FideId}");
                using var doc = JsonDocument.Parse(json);
                var r = doc.RootElement;

                p.Standard = ReadInt(r, "standard") ?? p.Standard;
                p.Rapid = ReadInt(r, "rapid") ?? p.Rapid;
                p.Blitz = ReadInt(r, "blitz") ?? p.Blitz;

                if (r.TryGetProperty("federation", out var fed) && fed.ValueKind == JsonValueKind.String)
                    p.Federation = fed.GetString();
                if (r.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                    p.Title = t.GetString();

                p.RatingsUpdatedUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                LogService.Write("Chess", "Warning", "Пропущена трансляция: " + ex.Message);
            }

            await Task.Delay(300);
        }

        await db.SaveChangesAsync();
    }

    private static int? ReadInt(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n)
            ? n : null;
    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ArenaTrack/0.1 (student project)");
        return client;
    }

    public async Task<int> SyncAsync()
    {
        var json = await Http.GetStringAsync("https://lichess.org/api/broadcast/top");
        using var doc = JsonDocument.Parse(json);

        var entries = new List<JsonElement>();
        foreach (var section in new[] { "active", "upcoming" })
            if (doc.RootElement.TryGetProperty(section, out var arr) && arr.ValueKind == JsonValueKind.Array)
                entries.AddRange(arr.EnumerateArray());

        using var db = new AppDbContext();
        int count = 0;

        foreach (var entry in entries.Take(5))
        {
            try
            {
                var tour = entry.GetProperty("tour");
                var round = entry.GetProperty("round");

                var tourId = tour.GetProperty("id").GetString()!;
                var tourName = tour.GetProperty("name").GetString() ?? "Без названия";
                var roundId = round.GetProperty("id").GetString()!;
                bool ongoing = round.TryGetProperty("ongoing", out var o) && o.GetBoolean();

                var startUtc = round.TryGetProperty("startsAt", out var s)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(s.GetInt64()).UtcDateTime
                    : DateTime.UtcNow;

                var tournament = await db.Tournaments
                    .FirstOrDefaultAsync(t => t.ExternalId == tourId);
                if (tournament == null)
                {
                    tournament = new Tournament
                    {
                        ExternalId = tourId,
                        Game = "Chess",
                        PrizePool = null,
                        StartDate = startUtc,
                        EndDate = startUtc
                    };
                    db.Tournaments.Add(tournament);
                }
                tournament.Name = tourName;
                await db.SaveChangesAsync();

                var pgn = await Http.GetStringAsync(
                    $"https://lichess.org/api/broadcast/round/{roundId}.pgn");

                foreach (var game in SplitGames(pgn))
                {
                    var h = ReadHeaders(game);
                    if (!h.TryGetValue("White", out var white) ||
                        !h.TryGetValue("Black", out var black)) continue;

                    UpsertPlayer(db, white, h, "White");
                    UpsertPlayer(db, black, h, "Black");

                    h.TryGetValue("Result", out var result);
                    var externalId = h.TryGetValue("GameURL", out var url)
                        ? url : $"{roundId}:{white}-{black}";

                    var match = await db.Matches
                        .FirstOrDefaultAsync(m => m.ExternalId == externalId);
                    if (match == null)
                    {
                        match = new MatchModel { ExternalId = externalId, Game = "Chess" };
                        db.Matches.Add(match);
                    }

                    match.TournamentId = tournament.Id;
                    match.Team1 = white;
                    match.Team2 = black;
                    match.StartTimeUtc = startUtc;
                    ApplyResult(match, result, ongoing);
                    count++;
                }

                await db.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
        }

        try { await SyncRatingsAsync(); } catch { }

        return count;
    }

    internal static void ApplyResult(MatchModel m, string? result, bool roundOngoing)
    {
        switch (result)
        {
            case "1-0": m.Status = "Finished"; m.ResultText = "1 : 0"; break;
            case "0-1": m.Status = "Finished"; m.ResultText = "0 : 1"; break;
            case "1/2-1/2": m.Status = "Finished"; m.ResultText = "½ : ½"; break;
            default: m.Status = roundOngoing ? "Live" : "Upcoming"; m.ResultText = null; break;
        }
    }

    internal static string[] SplitGames(string pgn) =>
        Regex.Split(pgn.Trim(), @"\r?\n\r?\n(?=\[Event )");

    internal static Dictionary<string, string> ReadHeaders(string game)
    {
        var dict = new Dictionary<string, string>();
        foreach (RegexMatch m in Regex.Matches(game, @"\[(\w+)\s+""([^""]*)""\]"))
            dict[m.Groups[1].Value] = m.Groups[2].Value;
        return dict;
    }
}