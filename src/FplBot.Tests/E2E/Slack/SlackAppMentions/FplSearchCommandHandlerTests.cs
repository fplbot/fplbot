using Fpl.Search.Models;

namespace FplBot.Tests.E2E.Slack.SlackAppMentions;

[Collection("AppSearch")]
public class FplSearchCommandHandlerTests(SearchAppFixture fixture)
{
    [Fact]
    public async Task SearchForSkjelbek()
    {
        await fixture.SeedSearchEntry(new EntryItem { Id = 1, RealName = "Magnus Skjelbek", TeamName = "Kun Magüero" });

        await fixture.AskSlackbot("<@UREFQD887> search skjelbek");
        var response = await fixture.SlackCapture.WaitForMessageAsync();

        Assert.Contains("Matching teams:", response.Text);
        Assert.Contains("Magnus Skjelbek", response.Text);
    }
}
