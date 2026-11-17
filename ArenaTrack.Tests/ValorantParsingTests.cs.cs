using System.Text.Json;
using ArenaTrack.Data;
using ArenaTrack.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArenaTrack.Tests;

public class ValorantParsingTests : IDisposable
{
    private readonly TestDb _testDb = new();
    public void Dispose() => _testDb.Dispose();

    private static string Json(string status = "finished", int opponents = 2,
                               string begin = "2026-11-10T12:00:00Z") =>
        $$"""
        {
          "id": 1001, "status": "{{status}}", "begin_at": "{{begin}}",
          "league": { "name": "VCT" },
          "tournament": { "id": 55, "name": "Playoffs",
                          "prizepool": "$100 000 United States Dollar",
                          "begin_at": "2026-11-01T00:00:00Z", "end_at": "2026-11-20T00:00:00Z" },
          "opponents": [
            {{(opponents >= 1 ? """{ "opponent": { "id": 1, "name": "Alpha" } }""" : "")}}
            {{(opponents >= 2 ? """, { "opponent": { "id": 2, "name": "Beta" } }""" : "")}}
          ],
          "results": [ { "team_id": 1, "score": 2 }, { "team_id": 2, "score": 1 } ]
        }
        """;

    [Fact]
    public async Task SaveMatch_StoresTeamsScoreTournamentAndPrize()
    {
        using var doc = JsonDocument.Parse(Json());
        using (var db = new AppDbContext())
        {
            bool saved = await ValorantService.SaveMatchAsync(db, doc.RootElement);
            await db.SaveChangesAsync();
            Assert.True(saved);
        }

        using var check = new AppDbContext();
        var m = check.Matches.Include(x => x.Tournament).Single();

        Assert.Equal("Alpha", m.Team1);
        Assert.Equal("Beta", m.Team2);
        Assert.Equal(2, m.Score1);
        Assert.Equal(1, m.Score2);
        Assert.Equal("Finished", m.Status);
        Assert.Equal("pandascore:1001", m.ExternalId);
        Assert.Equal("VCT Playoffs", m.Tournament!.Name);
        Assert.Equal("$100 000 United States Dollar", m.Tournament.PrizePool);
        Assert.Equal(2, check.Teams.Count());
    }

    [Fact]
    public async Task SaveMatch_SameMatchTwice_DoesNotCreateDuplicates()
    {
        using var doc = JsonDocument.Parse(Json());

        for (int i = 0; i < 2; i++)
        {
            using var db = new AppDbContext();
            await ValorantService.SaveMatchAsync(db, doc.RootElement);
            await db.SaveChangesAsync();
        }

        using var check = new AppDbContext();
        Assert.Equal(1, check.Matches.Count());
        Assert.Equal(1, check.Tournaments.Count());
        Assert.Equal(2, check.Teams.Count());
    }

    [Fact]
    public async Task SaveMatch_WithoutTwoOpponents_IsSkipped()
    {
        using var doc = JsonDocument.Parse(Json(opponents: 1));
        using var db = new AppDbContext();

        bool saved = await ValorantService.SaveMatchAsync(db, doc.RootElement);

        Assert.False(saved);
    }

    [Fact]
    public async Task SaveMatch_CanceledMatch_IsSkipped()
    {
        using var doc = JsonDocument.Parse(Json(status: "canceled"));
        using var db = new AppDbContext();

        Assert.False(await ValorantService.SaveMatchAsync(db, doc.RootElement));
    }

    [Fact]
    public async Task SaveMatch_RunningMatchStartedLongAgo_IsMarkedFinished()
    {
        var longAgo = DateTime.UtcNow.AddHours(-30).ToString("o");
        using var doc = JsonDocument.Parse(Json(status: "running", begin: longAgo));
        using (var db = new AppDbContext())
        {
            await ValorantService.SaveMatchAsync(db, doc.RootElement);
            await db.SaveChangesAsync();
        }

        using var check = new AppDbContext();
        Assert.Equal("Finished", check.Matches.Single().Status);   //защита от зависших матчей
    }

    [Fact]
    public void ReadDate_ConvertsToUtc_AndHandlesMissingField()
    {
        using var doc = JsonDocument.Parse("""{ "begin_at": "2026-11-10T15:00:00+03:00" }""");

        var date = ValorantService.ReadDate(doc.RootElement, "begin_at");
        var missing = ValorantService.ReadDate(doc.RootElement, "end_at");

        Assert.Equal(new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc), date);
        Assert.Null(missing);
    }
}