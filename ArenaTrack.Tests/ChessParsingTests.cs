using ArenaTrack.Services;
using Xunit;
using MatchModel = ArenaTrack.Models.Match;

namespace ArenaTrack.Tests;

public class ChessParsingTests
{
    private const string Pgn =
        "[Event \"Round 1\"]\n[White \"Nakamura, Hikaru\"]\n[Black \"Gukesh, D\"]\n" +
        "[Result \"1-0\"]\n[WhiteFideId \"2016192\"]\n[WhiteElo \"2802\"]\n\n1. e4 e5 1-0\n\n" +
        "[Event \"Round 1\"]\n[White \"Caruana, Fabiano\"]\n[Black \"Praggnanandhaa R\"]\n" +
        "[Result \"*\"]\n\n1. d4 d5 *";

    [Fact]
    public void SplitGames_SplitsPgnIntoSeparateGames()
    {
        var games = ChessService.SplitGames(Pgn);

        Assert.Equal(2, games.Length);
        Assert.Contains("Nakamura", games[0]);
        Assert.Contains("Caruana", games[1]);
    }

    [Fact]
    public void ReadHeaders_ParsesPlayersResultAndFideId()
    {
        var games = ChessService.SplitGames(Pgn);

        var h = ChessService.ReadHeaders(games[0]);

        Assert.Equal("Nakamura, Hikaru", h["White"]);
        Assert.Equal("Gukesh, D", h["Black"]);
        Assert.Equal("1-0", h["Result"]);
        Assert.Equal("2016192", h["WhiteFideId"]);
    }

    [Theory]
    [InlineData("1-0", false, "Finished", "1 : 0")]
    [InlineData("0-1", false, "Finished", "0 : 1")]
    [InlineData("1/2-1/2", false, "Finished", "½ : ½")]
    [InlineData("*", true, "Live", null)]
    [InlineData("*", false, "Upcoming", null)]
    [InlineData(null, true, "Live", null)]
    public void ApplyResult_MapsResultAndRoundState(
        string? result, bool ongoing, string expectedStatus, string? expectedText)
    {
        var m = new MatchModel();

        ChessService.ApplyResult(m, result, ongoing);

        Assert.Equal(expectedStatus, m.Status);
        Assert.Equal(expectedText, m.ResultText);
    }
}