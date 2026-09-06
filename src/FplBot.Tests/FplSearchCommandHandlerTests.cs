using FplBot.Tests.Helpers;
using FplBot.WebApi.Slack.Handlers.SlackEvents;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Tests;

public class FplSearchCommandHandlerTests(ITestOutputHelper logger)
{
    private readonly IHandleAppMentions _client = Factory.GetHandler<FplSearchHandler>(logger);

    [Fact(Skip = "integration")]
    public async Task SearchForSkjelbek()
    {
        var dummy = Factory.CreateDummyEvent("search skjelbek");
        var playerData = await _client.Handle(dummy.meta, dummy.@event);
        Assert.Equal("Matching teams:\n" +
                     "▪️ <https://fantasy.premierleague.com/entry/192197/event/9|Kun Magüero> (Magnus Skjelbek)\n" +
                     "▪️ <https://fantasy.premierleague.com/entry/76744/event/9|van de skjelbeek> (Lars Skjelbek)\n" +
                     "▪️ <https://fantasy.premierleague.com/entry/3558015/event/9|Anders Balleklubb> (Anders Skjelbek)\n\n" +
                     "Matching leagues:\nFound no matching leagues 🤷‍♂️", playerData.Response);
    }
}
