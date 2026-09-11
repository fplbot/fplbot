using FplBot.Data.Slack;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Slack.Handlers.SlackEvents;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Tests;

public class FplTransfersCommandHandlerTests(ITestOutputHelper logger)
{
    private readonly (IHandleAppMentions Handler, SlackTeam Team) _client = Factory.GetHandler<FplTransfersCommandHandler>(logger);

    [Theory]
    [InlineData("@fplbot transfers")]
    [InlineData("<@UREFQD887> transfers")]
    public async Task GetTransfersHandlerShouldPostTransfers(string input)
    {
        var dummy = Factory.CreateDummyEvent(_client.Team, input);
        var transfers = await _client.Handler.Handle(dummy.meta, dummy.@event);
        Assert.Contains("Transfers", transfers.Response, StringComparison.InvariantCultureIgnoreCase);
    }

    [Theory]
    [InlineData("<@UREFQD887> transfers 20")]
    public async Task GetTransfersForExplicitGwShouldPostTransfersForGameweek(string input)
    {
        var dummy = Factory.CreateDummyEvent(_client.Team, input);
        var transfers = await _client.Handler.Handle(dummy.meta, dummy.@event);
        Assert.Contains("Transfers", transfers.Response, StringComparison.InvariantCultureIgnoreCase);
    }

    [Theory]
    [InlineData("@fplbot transfers 1")]
    [InlineData("<@UREFQD887> transfers 1")]
    public async Task GetTransfersHandlerForGw1ShouldPostSpecialMessage(string input)
    {
        var dummy = Factory.CreateDummyEvent(_client.Team, input);
        var transfers = await _client.Handler.Handle(dummy.meta, dummy.@event);

        Assert.Contains("Transfers", transfers.Response, StringComparison.InvariantCultureIgnoreCase);
    }
}
