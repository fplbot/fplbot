using FplBot.Tests.Helpers;
using FplBot.WebApi.Slack.Handlers.SlackEvents;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Tests;

public class FplPlayerCommandHandlerTests(ITestOutputHelper logger)
{
    private readonly IHandleAppMentions _client = Factory.GetHandler<FplPlayerCommandHandler>(logger);

    [Theory]
    [InlineData("@fplbot player salah")]
    [InlineData("<@UREFQD887> player salah")]
    public async Task GetPlayerHandler(string input)
    {
        var dummy = Factory.CreateDummyEvent(input);
        var playerData = await _client.Handle(dummy.meta, dummy.@event);
        Assert.Contains("Found matching player for salah", playerData.Response);
    }

    [Theory]
    [InlineData("@fplbot player ", "nonexistant")]
    [InlineData("<@UREFQD887> player ", "nonexistant")]
    public async Task GetPlayerHandlerNonPlayer(string input, string player)
    {
        var dummy = Factory.CreateDummyEvent($"{input}{player}");
        var playerData = await _client.Handle(dummy.meta, dummy.@event);

        Assert.Equal("Found no matching player for nonexistant: ", playerData.Response);
    }

    [Theory]
    [InlineData("robertson", "Andrew Robertson")]
    [InlineData("van dijk", "Virgil van Dijk")]
    [InlineData("vandijk", "Virgil van Dijk")]
    [InlineData("dijk", "Virgil van Dijk")]
    [InlineData("becker", "Alisson Becker")]
    [InlineData("alisson", "Alisson Becker")]
    public async Task GetPlayer(string input, string expectedPlayer)
    {
        var dummy = Factory.CreateDummyEvent($"player {input}");
        var playerData = await _client.Handle(dummy.meta, dummy.@event);
        Assert.Equal($"Found matching player for {input}: {expectedPlayer}", playerData.Response);
    }
}
