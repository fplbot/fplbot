using FplBot.Slack.Handlers.SlackEvents;
using FplBot.Tests.Helpers;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Tests;

public class FplInjuryCommandHandlerTests
{
    private readonly IHandleAppMentions _client;

    public FplInjuryCommandHandlerTests(ITestOutputHelper logger)
    {
        _client = Factory.GetHandler<FplInjuryCommandHandler>(logger);
    }

    [Theory]
    [InlineData("@fplbot injuries")]
    public async Task GetPlayerHandler(string input)
    {
        var dummyEvent = Factory.CreateDummyEvent(input);
        var playerData = await _client.Handle(dummyEvent.meta, dummyEvent.@event);
        Assert.NotEmpty(playerData.Response);
    }
}
