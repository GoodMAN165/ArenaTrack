using ArenaTrack.Services;
using Xunit;

namespace ArenaTrack.Tests;

public class StatsServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    private static string Val(ProfileData p, string label) =>
        p.Stats.First(s => s.Label == label).Value;

    [Fact]
    public void GetNames_ReturnsDistinctSortedNames_WithoutPlaceholder()
    {
        int t = _db.AddTournament("Valorant");
        _db.AddMatch(t, "Valorant", "Beta", "Alpha", "Upcoming", -1);
        _db.AddMatch(t, "Valorant", "Alpha", "?", "Upcoming", -2);

        var names = StatsService.GetNames("Valorant");

        Assert.Equal(new[] { "Alpha", "Beta" }, names);
    }

    [Fact]
    public void GetNames_FiltersByGame()
    {
        int v = _db.AddTournament("Valorant");
        int c = _db.AddTournament("Chess");
        _db.AddMatch(v, "Valorant", "Team A", "Team B", "Upcoming", -1);
        _db.AddMatch(c, "Chess", "Player X", "Player Y", "Upcoming", -1);

        var names = StatsService.GetNames("Chess");

        Assert.Equal(new[] { "Player X", "Player Y" }, names);
    }

    [Fact]
    public void Profile_CountsWinsLossesAndWinrate_FromTeamPerspective()
    {
        int t = _db.AddTournament("Valorant");
        // A выигрывает как первая команда, выигрывает как вторая, проигрывает
        _db.AddMatch(t, "Valorant", "A", "B", "Finished", 72, 2, 1);
        _db.AddMatch(t, "Valorant", "C", "A", "Finished", 48, 0, 2);
        _db.AddMatch(t, "Valorant", "A", "D", "Finished", 24, 1, 2);

        var p = StatsService.GetProfile("A", "Valorant");

        Assert.Equal("3", Val(p, "Матчей"));
        Assert.Equal("2", Val(p, "Побед"));
        Assert.Equal("1", Val(p, "Поражений"));
        Assert.Equal("67%", Val(p, "Винрейт"));
        Assert.Equal("Поражения: 1", Val(p, "Серия"));   //последний матч проигран
    }

    [Fact]
    public void Profile_ShowsScoreFromSelectedTeamSide()
    {
        int t = _db.AddTournament("Valorant");
        _db.AddMatch(t, "Valorant", "A", "B", "Finished", 72, 2, 1);
        _db.AddMatch(t, "Valorant", "C", "A", "Finished", 48, 0, 2);

        var p = StatsService.GetProfile("A", "Valorant");

        //Строки идут от новых матчей к старым
        Assert.Equal("C", p.Rows[0].Opponent);
        Assert.Equal("2 : 0", p.Rows[0].Score);     //счёт перевёрнут очки A слева
        Assert.Equal("Победа", p.Rows[0].Result);
    }

    [Fact]
    public void Profile_IgnoresUnfinishedMatchesInStats_ButListsThem()
    {
        int t = _db.AddTournament("Valorant");
        _db.AddMatch(t, "Valorant", "A", "B", "Finished", 48, 2, 0);
        _db.AddMatch(t, "Valorant", "A", "C", "Upcoming", -5);

        var p = StatsService.GetProfile("A", "Valorant");

        Assert.Equal("1", Val(p, "Матчей"));
        Assert.Equal(2, p.Rows.Count);
        Assert.Contains(p.Rows, r => r.Result == "Предстоит");
    }

    [Fact]
    public void Profile_StreakCountsConsecutiveWins()
    {
        int t = _db.AddTournament("Valorant");
        _db.AddMatch(t, "Valorant", "A", "B", "Finished", 96, 0, 2);   //старое поражение
        _db.AddMatch(t, "Valorant", "A", "C", "Finished", 72, 2, 0);
        _db.AddMatch(t, "Valorant", "A", "D", "Finished", 48, 2, 1);
        _db.AddMatch(t, "Valorant", "E", "A", "Finished", 24, 0, 2);

        var p = StatsService.GetProfile("A", "Valorant");

        Assert.Equal("Победы: 3", Val(p, "Серия"));
    }

    [Fact]
    public void Profile_WithoutFinishedMatches_ShowsDashes()
    {
        int t = _db.AddTournament("Valorant");
        _db.AddMatch(t, "Valorant", "A", "B", "Upcoming", -3);

        var p = StatsService.GetProfile("A", "Valorant");

        Assert.Equal("0", Val(p, "Матчей"));
        Assert.Equal("—", Val(p, "Винрейт"));
        Assert.Equal("—", Val(p, "Серия"));
    }

    [Fact]
    public void ChessProfile_CountsDraws_AndHasDrawsTile()
    {
        int t = _db.AddTournament("Chess");
        _db.AddMatch(t, "Chess", "X", "Y", "Finished", 72, result: "1 : 0");   //X выиграл
        _db.AddMatch(t, "Chess", "Z", "X", "Finished", 48, result: "½ : ½");   //ничья
        _db.AddMatch(t, "Chess", "Y", "X", "Finished", 24, result: "1 : 0");   //X проиграл чёрными

        var p = StatsService.GetProfile("X", "Chess");

        Assert.Equal("3", Val(p, "Матчей"));
        Assert.Equal("1", Val(p, "Побед"));
        Assert.Equal("1", Val(p, "Поражений"));
        Assert.Equal("1", Val(p, "Ничьих"));
        Assert.Equal("33%", Val(p, "Винрейт"));
    }

    [Fact]
    public void ValorantProfile_HasNoDrawsTile()
    {
        int t = _db.AddTournament("Valorant");
        _db.AddMatch(t, "Valorant", "A", "B", "Finished", 24, 2, 1);

        var p = StatsService.GetProfile("A", "Valorant");

        Assert.DoesNotContain(p.Stats, s => s.Label == "Ничьих");
    }
}