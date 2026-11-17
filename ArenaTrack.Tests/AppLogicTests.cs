using ArenaTrack.Data;
using ArenaTrack.Models;
using ArenaTrack.Services;
using Xunit;
using MatchModel = ArenaTrack.Models.Match;

namespace ArenaTrack.Tests;

public class FavoriteServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    [Fact]
    public void Toggle_AddsThenRemoves()
    {
        Assert.True(FavoriteService.Toggle(42));
        Assert.Contains(42, FavoriteService.GetIds());

        Assert.False(FavoriteService.Toggle(42));
        Assert.DoesNotContain(42, FavoriteService.GetIds());
    }

    [Fact]
    public void Toggle_RaisesChangedEvent()
    {
        int calls = 0;
        void Handler() => calls++;
        FavoriteService.Changed += Handler;
        try { FavoriteService.Toggle(7); }
        finally { FavoriteService.Changed -= Handler; }

        Assert.Equal(1, calls);
    }
}

public class DemoDataTests : IDisposable
{
    private readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    [Fact]
    public void Load_AddsDemoMatches_AndIsIdempotent()
    {
        DemoData.Load();
        DemoData.Load();//повторная загрузка не должна дублировать

        using var db = new AppDbContext();
        Assert.True(DemoData.IsLoaded());
        Assert.Equal(11, db.Matches.Count());
        Assert.Equal(2, db.Tournaments.Count());
    }

    [Fact]
    public void Remove_DeletesOnlyDemoData()
    {
        int t = _db.AddTournament("Valorant", "Real Cup");
        _db.AddMatch(t, "Valorant", "A", "B", "Finished", 5, 2, 0);
        DemoData.Load();

        DemoData.Remove();

        using var db = new AppDbContext();
        Assert.False(DemoData.IsLoaded());
        Assert.Equal(1, db.Matches.Count());//реальный матч остался
        Assert.Equal("Real Cup", db.Tournaments.Single().Name);
    }

    [Fact]
    public void Remove_AlsoRemovesFavoritesOfDemoMatches()
    {
        DemoData.Load();
        int demoId;
        using (var db = new AppDbContext()) demoId = db.Matches.First().Id;
        FavoriteService.Toggle(demoId);

        DemoData.Remove();

        Assert.Empty(FavoriteService.GetIds());
    }
}

public class PasswordServiceTests : IDisposable
{
    private readonly string _file =
        Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid():N}.json");

    public PasswordServiceTests()
    {
        AppSettings.PathOverride = _file;//чтобы не затронуть настоящие настройки
        AppSettings.PasswordHash = null;
        AppSettings.PasswordSalt = null;
        PasswordService.IsUnlocked = false;
    }

    public void Dispose()
    {
        AppSettings.PasswordHash = null;
        AppSettings.PasswordSalt = null;
        AppSettings.PathOverride = null;
        PasswordService.IsUnlocked = false;
        try { File.Delete(_file); } catch { }
    }

    [Fact]
    public void Verify_WithoutPassword_ReturnsFalse()
    {
        Assert.False(PasswordService.HasPassword);
        Assert.False(PasswordService.Verify("anything"));
    }

    [Fact]
    public void SetPassword_ThenVerify_AcceptsOnlyCorrectPassword()
    {
        PasswordService.SetPassword("secret123");

        Assert.True(PasswordService.HasPassword);
        Assert.True(PasswordService.Verify("secret123"));
        Assert.False(PasswordService.Verify("Secret123"));
        Assert.False(PasswordService.Verify(""));
    }

    [Fact]
    public void Password_IsNotStoredInPlainText()
    {
        PasswordService.SetPassword("secret123");

        Assert.DoesNotContain("secret123", AppSettings.PasswordHash);
        Assert.DoesNotContain("secret123", File.ReadAllText(_file));
    }

    [Fact]
    public void SamePasswordTwice_GivesDifferentHashes_BecauseOfSalt()
    {
        PasswordService.SetPassword("secret123");
        var first = AppSettings.PasswordHash;

        PasswordService.SetPassword("secret123");

        Assert.NotEqual(first, AppSettings.PasswordHash);
        Assert.True(PasswordService.Verify("secret123"));
    }
}

public class ModelTests
{
    [Theory]
    [InlineData(null, "не указан")]
    [InlineData("", "не указан")]
    [InlineData("   ", "не указан")]
    [InlineData("$1 000", "$1 000")]
    public void Tournament_PrizeText_ShowsPlaceholderWhenMissing(string? prize, string expected)
    {
        var t = new Tournament { PrizePool = prize };
        Assert.Equal(expected, t.PrizeText);
    }

    [Fact]
    public void Match_ScoreText_UsesResultTextThenScoreThenVs()
    {
        Assert.Equal("½ : ½", new MatchModel { ResultText = "½ : ½" }.ScoreText);
        Assert.Equal("2 : 1", new MatchModel { Score1 = 2, Score2 = 1 }.ScoreText);
        Assert.Equal("vs", new MatchModel().ScoreText);
    }

    [Theory]
    [InlineData("Live", "Идёт")]
    [InlineData("Finished", "Завершён")]
    [InlineData("Upcoming", "Предстоит")]
    [InlineData("что-то-другое", "Предстоит")]
    public void Match_StatusText_IsLocalized(string status, string expected)
    {
        Assert.Equal(expected, new MatchModel { Status = status }.StatusText);
    }

    [Fact]
    public void Match_IsFavorite_RaisesPropertyChanged_OnlyOnRealChange()
    {
        var m = new MatchModel();
        var raised = new List<string?>();
        m.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        m.IsFavorite = true;
        m.IsFavorite = true;   // то же значение: событие не нужно

        Assert.Single(raised);
        Assert.Equal(nameof(MatchModel.IsFavorite), raised[0]);
    }
}