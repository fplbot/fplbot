using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.Handlers.SlackAppMentions;

// Dispatches through the real ISelectAppMentionEventHandlers selection logic (the same one the
// real Slack webhook uses to pick a handler) instead of hand-picking a handler instance via DI —
// asserts on the handler's returned response, not on any Slack client call (FplPlayerCommandHandler
// doesn't post to Slack directly; it just returns a response string for the webhook to reply with).
[Collection("App")]
public class FplPlayerCommandHandlerTests(AppFixture fixture)
{
    private async Task<string> Ask(string input)
    {
        var team = SlackTeamFaker.Generate();
        await fixture.Store.Insert(team);
        var dummy = Factory.CreateDummyEvent(team, input);

        var response = await fixture.DispatchAppMention(dummy.meta, dummy.@event);
        return response.Response;
    }

    [Theory]
    [InlineData("@fplbot player salah")]
    [InlineData("<@UREFQD887> player salah")]
    public async Task GetPlayerHandler(string input)
    {
        var response = await Ask(input);
        Assert.Contains("Found matching player for salah", response);
    }

    [Theory]
    [InlineData("@fplbot player ", "nonexistant")]
    [InlineData("<@UREFQD887> player ", "nonexistant")]
    public async Task GetPlayerHandlerNonPlayer(string input, string player)
    {
        var response = await Ask($"{input}{player}");
        Assert.Equal("Found no matching player for nonexistant: ", response);
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
        var response = await Ask($"<@UREFQD887> player {input}");
        Assert.Equal($"Found matching player for {input}: {expectedPlayer}", response);
    }
}
