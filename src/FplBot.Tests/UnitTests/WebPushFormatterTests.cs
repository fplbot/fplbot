using FplBot.EventHandlers.Web;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.UnitTests;

public class WebPushFormatterTests
{
    [Fact]
    public void Goal_PutsRunningScoreInTitleAndScorerInBody()
    {
        var score = new FixtureScore(
            new FixtureTeam(1, "Manchester City", "MCI"),
            new FixtureTeam(2, "Arsenal", "ARS"), 34, 2, 0);

        var (title, body) = WebPushFormatter.Goal(score, "Foden");

        Assert.Equal("⚽ MCI 2-0 ARS", title);
        Assert.Contains("Foden", body);
    }

    [Fact]
    public void InjuryUpdates_CountsThePlayers()
    {
        var (title, body) = WebPushFormatter.InjuryUpdates(3);

        Assert.Contains("Injury", title);
        Assert.Contains("3", body);
    }

    [Fact]
    public void Standings_NamesTheGameweek()
    {
        var (title, body) = WebPushFormatter.Standings(12);

        Assert.Contains("12", title + body);
    }
}
