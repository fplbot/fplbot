using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Tests.E2E;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplSubscribeCommandHandlerTests(AppFixture fixture)
{
    private ISlackTeamRepository Repo => fixture.Services.GetRequiredService<ISlackTeamRepository>();

    [Fact]
    public async Task SubscribingPersistsToThatChannel()
    {
        var team = await fixture.SeedTeam();

        await fixture.AskSlackbot(team, "<@UREFQD887> subscribe Standings,Deadlines");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("Updated subscriptions", response.Text, StringComparison.InvariantCultureIgnoreCase);

        var installation = await Repo.GetInstallation(team.TeamId!);
        var channel = installation.GetChannel(team.FplBotSlackChannel!);
        Assert.NotNull(channel);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public async Task SubscribingTwiceAccumulatesRatherThanReplacesExistingSubscription()
    {
        var team = await fixture.SeedTeam();

        await fixture.AskSlackbot(team, "<@UREFQD887> subscribe Standings");
        await fixture.SlackCapture.WaitForMessageAsync();

        await fixture.AskSlackbot(team, "<@UREFQD887> subscribe Deadlines");
        await fixture.SlackCapture.WaitForMessageAsync();

        var installation = await Repo.GetInstallation(team.TeamId!);
        var channel = installation.GetChannel(team.FplBotSlackChannel!)!;
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    // A channel that has never been followed or subscribed to before should get its own,
    // independent subscription record rather than requiring @fplbot follow first.
    [Fact]
    public async Task SubscribingInAFreshChannelCreatesAnIndependentSubscription()
    {
        var team = await fixture.SeedTeam();
        const string newChannelId = "#brand-new-channel";
        var messageInNewChannel = new SlackTeam { TeamId = team.TeamId, FplBotSlackChannel = newChannelId };

        await fixture.AskSlackbot(messageInNewChannel, "<@UREFQD887> subscribe Standings");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("Updated subscriptions", response.Text, StringComparison.InvariantCultureIgnoreCase);

        var installation = await Repo.GetInstallation(team.TeamId!);

        var newChannel = installation.GetChannel(newChannelId);
        Assert.NotNull(newChannel);
        Assert.True(newChannel.IsSubscribedTo(FplEvent.Standings));

        var originalChannel = installation.GetChannel(team.FplBotSlackChannel!);
        Assert.NotNull(originalChannel);
        Assert.False(originalChannel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public async Task UnsubscribingRemovesOnlyThatEvent()
    {
        var team = await fixture.SeedTeam();

        await fixture.AskSlackbot(team, "<@UREFQD887> subscribe Standings,Deadlines");
        await fixture.SlackCapture.WaitForMessageAsync();

        await fixture.AskSlackbot(team, "<@UREFQD887> unsubscribe Standings");
        await fixture.SlackCapture.WaitForMessageAsync();

        var installation = await Repo.GetInstallation(team.TeamId!);
        var channel = installation.GetChannel(team.FplBotSlackChannel!)!;
        Assert.False(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public async Task NoArgumentsPromptsForArguments()
    {
        var team = await fixture.SeedTeam();

        await fixture.AskSlackbot(team, "<@UREFQD887> subscribe");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("You need to pass some arguments", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }
}
