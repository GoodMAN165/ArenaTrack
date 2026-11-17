using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ArenaTrack.Data;
using ArenaTrack.Models;
using MatchModel = ArenaTrack.Models.Match;

namespace ArenaTrack.Services;

public class ValorantService
{
    private static async Task UpsertTeamAsync(AppDbContext db, JsonElement t)
    {
        var ext = "pandascore:" + t.GetProperty("id").GetInt64();
        var team = db.Teams.Local.FirstOrDefault(x => x.ExternalId == ext)
                   ?? await db.Teams.FirstOrDefaultAsync(x => x.ExternalId == ext);
        if (team == null)
        {
            team = new Team { ExternalId = ext, Game = "Valorant" };
            db.Teams.Add(team);
        }
        team.Name = t.GetProperty("name").GetString() ?? "?";
    }
    private static async Task SyncRostersAsync()
    {
        using var db = new AppDbContext();
        var limit = DateTime.UtcNow.AddHours(-24);

        var all = await db.Teams.Where(t => t.Game == "Valorant" && t.ExternalId != null).ToListAsync();
        var due = all.Where(t => t.RosterUpdatedUtc == null || t.RosterUpdatedUtc < limit)
                     .Take(50).ToList();
        if (due.Count == 0) return;

        var ids = string.Join(",", due.Select(t => t.ExternalId!.Replace("pandascore:", "")));
        var url = $"https://api.pandascore.co/valorant/teams?{Uri.EscapeDataString("filter[id]")}={ids}&per_page=50";

        var json = await Http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(json);

        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var ext = "pandascore:" + el.GetProperty("id").GetInt64();
            var team = due.FirstOrDefault(t => t.ExternalId == ext);
            if (team == null) continue;

            db.RosterPlayers.RemoveRange(db.RosterPlayers.Where(p => p.TeamId == team.Id));

            if (el.TryGetProperty("players", out var players) && players.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in players.EnumerateArray())
                {
                    var nick = Str(p, "name");
                    if (nick.Length == 0) continue;

                    db.RosterPlayers.Add(new RosterPlayer
                    {
                        TeamId = team.Id,
                        Nickname = nick,
                        FullName = $"{Str(p, "first_name")} {Str(p, "last_name")}".Trim(),
                        Country = Str(p, "nationality")
                    });
                }
            }
        }
        foreach (var t in due) t.RosterUpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static string Str(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<int> SyncAsync()
    {
        var key = ApiKeyStore.Get();
       
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("API-ключ не задан. Введите его в разделе «Администрирование».");
        
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        using var db = new AppDbContext();
        int count = 0;
        var seenRunning = new HashSet<string>();

        string[] endpoints =
        {
            "https://api.pandascore.co/valorant/matches/running?per_page=20",
            "https://api.pandascore.co/valorant/matches/upcoming?per_page=20",
            "https://api.pandascore.co/valorant/matches/past?per_page=20"
        };

        foreach (var url in endpoints)
        {
            var json = await Http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            bool isRunning = url.Contains("/running");

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                try
                {
                    if (isRunning)
                        seenRunning.Add("pandascore:" + item.GetProperty("id").GetInt64());

                    if (await SaveMatchAsync(db, item)) count++;
                }
                catch (Exception ex)
                {
                    LogService.Write("Valorant", "Warning", "Пропущена запись: " + ex.Message);
                }
            }
            await db.SaveChangesAsync();
        }


        var stale = await db.Matches
            .Where(m => m.Game == "Valorant" && m.Status == "Live"
             && m.ExternalId != null && m.ExternalId.StartsWith("pandascore:"))
            .ToListAsync();
        foreach (var m in stale.Where(m => !seenRunning.Contains(m.ExternalId ?? "")))
            m.Status = "Finished";
        await db.SaveChangesAsync();

        try { await SyncRostersAsync(); } catch { }

        return count;
    }

    internal static async Task<bool> SaveMatchAsync(AppDbContext db, JsonElement item)
    {

        var opponents = item.GetProperty("opponents");
        if (opponents.GetArrayLength() != 2) return false;

        var team1 = opponents[0].GetProperty("opponent");
        var team2 = opponents[1].GetProperty("opponent");
        await UpsertTeamAsync(db, team1);
        await UpsertTeamAsync(db, team2);

        var status = item.GetProperty("status").GetString();
        if (status is "canceled") return false;


        var t = item.GetProperty("tournament");
        var tourExtId = "pandascore:" + t.GetProperty("id").GetInt64();
        var league = item.TryGetProperty("league", out var l) ? l.GetProperty("name").GetString() : null;
        var tourName = $"{league} {t.GetProperty("name").GetString()}".Trim();

        var tournament = db.Tournaments.Local.FirstOrDefault(x => x.ExternalId == tourExtId)
                         ?? await db.Tournaments.FirstOrDefaultAsync(x => x.ExternalId == tourExtId);
        if (tournament == null)
        {
            tournament = new Tournament { ExternalId = tourExtId, Game = "Valorant" };
            db.Tournaments.Add(tournament);
        }
        tournament.Name = tourName;
        tournament.PrizePool = t.TryGetProperty("prizepool", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() : null;
        tournament.StartDate = ReadDate(t, "begin_at") ?? DateTime.UtcNow;
        tournament.EndDate = ReadDate(t, "end_at") ?? tournament.StartDate;


        var extId = "pandascore:" + item.GetProperty("id").GetInt64();
        var match = db.Matches.Local.FirstOrDefault(x => x.ExternalId == extId)
                    ?? await db.Matches.FirstOrDefaultAsync(x => x.ExternalId == extId);
        if (match == null)
        {
            match = new MatchModel { ExternalId = extId, Game = "Valorant" };
            db.Matches.Add(match);
        }

        match.Tournament = tournament;
        match.Team1 = team1.GetProperty("name").GetString() ?? "?";
        match.Team2 = team2.GetProperty("name").GetString() ?? "?";
        match.StartTimeUtc = ReadDate(item, "begin_at") ?? ReadDate(item, "scheduled_at") ?? DateTime.UtcNow;


        match.Status = status switch
        {
            "running" => "Live",
            "finished" => "Finished",
            _ => "Upcoming"
        };

        if (match.Status == "Live" && match.StartTimeUtc < DateTime.UtcNow.AddHours(-12))
            match.Status = "Finished";


        match.Score1 = match.Score2 = null;
        match.ResultText = null;
        if (item.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            var id1 = team1.GetProperty("id").GetInt64();
            foreach (var r in results.EnumerateArray())
            {
                var score = r.GetProperty("score").GetInt32();
                if (r.GetProperty("team_id").GetInt64() == id1) match.Score1 = score;
                else match.Score2 = score;
            }
        }

        return true;
    }


    internal static DateTime? ReadDate(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(v.GetString(), out var dto))
            return dto.UtcDateTime;
        return null;
    }
}