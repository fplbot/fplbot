using FplBot.Tests.E2E;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplSearchCommandHandlerTests(AppFixture fixture)
{
    [Fact(Skip = "integration")]
    public async Task SearchForSkjelbek()
    {
        await fixture.AskSlackbot("search skjelbek");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Equal("Matching teams:\n" +
                     "▪️ <https://fantasy.premierleague.com/entry/192197/event/9|Kun Magüero> (Magnus Skjelbek)\n" +
                     "▪️ <https://fantasy.premierleague.com/entry/76744/event/9|van de skjelbeek> (Lars Skjelbek)\n" +
                     "▪️ <https://fantasy.premierleague.com/entry/3558015/event/9|Anders Balleklubb> (Anders Skjelbek)\n\n" +
                     "Matching leagues:\nFound no matching leagues 🤷‍♂️", response.Text);
    }
}
