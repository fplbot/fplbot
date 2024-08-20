using FplBot.Tests.Helpers;
using FplBot.WebApi.Slack.Handlers.SlackEvents;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Tests;

public class FplChangeLeagueIdHandlerTests
{
    private readonly ITestOutputHelper _logger;
    private readonly IHandleAppMentions _client;

    public FplChangeLeagueIdHandlerTests(ITestOutputHelper logger)
    {
        _logger = logger;
        _client = Factory.GetHandler<FplFollowLeagueHandler>(logger);
    }

    [Fact]
    public async Task ChangeLeagueIdShouldUpdate()
    {
        var dummy = Factory.CreateDummyEvent("follow 126199");
        var response = await _client.Handle(dummy.meta, dummy.@event);
        _logger.WriteLine(response.Response);
        Assert.Contains("Thanks! You're now following", response.Response, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ChangeToInvalidLeagueIdShouldNotUpdate()
    {
        var dummy = Factory.CreateDummyEvent("follow abc");
        var response = await _client.Handle(dummy.meta, dummy.@event);
        _logger.WriteLine(response.Response);
        Assert.Contains("Could not update league to id 'abc'. Make sure it's a single valid number.", response.Response, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ChangeToNotFoundLeagueIdShouldNotUpdate()
    {
        var dummy = Factory.CreateDummyEvent("follow 0");
        var response = await _client.Handle(dummy.meta, dummy.@event);
        _logger.WriteLine(response.Response);
        Assert.Contains("Could not find league 0 :/ Could you find it at https://fantasy.premierleague.com/leagues/0/standings/c ?", response.Response, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ChangeToMissingArgsProvidesHelpText()
    {
        var dummy = Factory.CreateDummyEvent("follow");
        var response = await _client.Handle(dummy.meta, dummy.@event);
        _logger.WriteLine(response.Response);
        Assert.Contains("No leagueId provided. Usage: `@fplbot follow 123`", response.Response, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ChangeToOtherNotFoundLeagueIdShouldNotUpdate()
    {
        var dummy = Factory.CreateDummyEvent("follow 11111111");
        var response = await _client.Handle(dummy.meta, dummy.@event);
        _logger.WriteLine(response.Response);
        Assert.Contains("Could not find league 11111111 :/ Could you find it at https://fantasy.premierleague.com/leagues/11111111/standings/c ?", response.Response, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task HandlesFormattedPhoneNumberLinks()
    {
        var dummy = Factory.CreateDummyEvent("follow <tel:1234|1234>");
        var response = await _client.Handle(dummy.meta, dummy.@event);
        _logger.WriteLine(response.Response);
        Assert.Contains("Thanks! You're now following", response.Response, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("leagueId: 1234", response.Response, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task HandlesMultipleNumbers()
    {
        var dummy = Factory.CreateDummyEvent("follow 1234 5678");
        var response = await _client.Handle(dummy.meta, dummy.@event);
        _logger.WriteLine(response.Response);
        Assert.Contains("Could not update league to id", response.Response, StringComparison.InvariantCultureIgnoreCase);
    }
}
