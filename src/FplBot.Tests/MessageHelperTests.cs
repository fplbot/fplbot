using FplBot.Slack.Helpers;

namespace FplBot.Tests;

public class MessageHelperTests
{

    [Fact]
    public void ExtractGameweekShouldExtractCorrectGameweek()
    {
        var result = MessageHelper.ExtractGameweek("transfers 12", "transfers {gw}");
        Assert.Equal(12, result);
    }

    [Fact]
    public void ExtractArgsShouldExtractCorrectPlayerName()
    {
        var result = MessageHelper.ExtractArgs("player kane", "player {args}");
        Assert.Equal("kane", result);
    }

    [Fact]
    public void ExtractArgsShouldExtractCorrectListOfSubscriptionEvents()
    {
        var result = MessageHelper.ExtractArgs("subscribe standings, transfers, captains", "subscribe {args}");
        Assert.Equal("standings, transfers, captains", result);
    }

    [Fact]
    public void ExtractArgsShouldExtractCorrectListOfUnsubscriptionEvents()
    {
        var result = MessageHelper.ExtractArgs("unsubscribe standings, transfers, captains", "subscribe {args}");
        Assert.Equal("standings, transfers, captains", result);
    }

    [Fact]
    public void ExtractArgsShouldExtractCorrectListWhenMultipleArgs()
    {
        var resultOne = MessageHelper.ExtractArgs("one standings, transfers, captains", new []{"one {args}", "two {args}"});
        Assert.Equal("standings, transfers, captains", resultOne);

        var resultTwo = MessageHelper.ExtractArgs("two standings, transfers, captains", new []{"one {args}", "two {args}"});
        Assert.Equal("standings, transfers, captains", resultTwo);
    }
}
