using FplBot.Data.Slack;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Slack.Handlers.SlackEvents;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Tests.Handlers.SlackAppMentions;

public class FplInjuryCommandHandlerTests(ITestOutputHelper logger)
{
    private readonly (IHandleAppMentions Handler, SlackTeam Team) _client = Factory.GetHandler<FplInjuryCommandHandler>(logger);

    [Theory]
    [InlineData("@fplbot injuries")]
    public async Task GetPlayerHandler(string input)
    {
        var dummyEvent = Factory.CreateDummyEvent(_client.Team, input);
        var playerData = await _client.Handler.Handle(dummyEvent.meta, dummyEvent.@event);
        Assert.NotEmpty(playerData.Response);
    }
}
