using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Tests.E2E;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplSubscribeCommandHandlerTests(AppFixture fixture)
{
    private ISlackTeamRepository Repo => fixture.Services.GetRequiredService<ISlackTeamRepository>();

    private static string ChannelOf(SlackInstallation installation) => installation.ChannelSubscriptions.First().ChannelId;

    [Fact]
    public async Task SubscribingPersistsToThatChannel()
    {
        var seeded = await fixture.SeedInstallation();
        var channelId = ChannelOf(seeded);

        await fixture.AskSlackbot(seeded, "<@UREFQD887> subscribe Standings,Deadlines");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("Updated subscriptions", response.Text, StringComparison.InvariantCultureIgnoreCase);

        var installation = await Repo.GetInstallation(seeded.TeamId);
        var channel = installation.GetChannel(channelId);
        Assert.NotNull(channel);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public async Task SubscribingTwiceAccumulatesRatherThanReplacesExistingSubscription()
    {
        var seeded = await fixture.SeedInstallation();
        var channelId = ChannelOf(seeded);

        await fixture.AskSlackbot(seeded, "<@UREFQD887> subscribe Standings");
        await fixture.SlackCapture.WaitForMessageAsync();

        await fixture.AskSlackbot(seeded, "<@UREFQD887> subscribe Deadlines");
        await fixture.SlackCapture.WaitForMessageAsync();

        var installation = await Repo.GetInstallation(seeded.TeamId);
        var channel = installation.GetChannel(channelId)!;
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    // A channel that has never been followed or subscribed to before should get its own,
    // independent subscription record rather than requiring @fplbot follow first.
    [Fact]
    public async Task SubscribingInAFreshChannelCreatesAnIndependentSubscription()
    {
        var seeded = await fixture.SeedInstallation();
        var originalChannelId = ChannelOf(seeded);
        const string newChannelId = "#brand-new-channel";

        await fixture.AskSlackbot(seeded.TeamId, newChannelId, "<@UREFQD887> subscribe Standings");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("Updated subscriptions", response.Text, StringComparison.InvariantCultureIgnoreCase);

        var installation = await Repo.GetInstallation(seeded.TeamId);

        var newChannel = installation.GetChannel(newChannelId);
        Assert.NotNull(newChannel);
        Assert.True(newChannel.IsSubscribedTo(FplEvent.Standings));

        var originalChannel = installation.GetChannel(originalChannelId);
        Assert.NotNull(originalChannel);
        Assert.False(originalChannel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public async Task UnsubscribingRemovesOnlyThatEvent()
    {
        var seeded = await fixture.SeedInstallation();
        var channelId = ChannelOf(seeded);

        await fixture.AskSlackbot(seeded, "<@UREFQD887> subscribe Standings,Deadlines");
        await fixture.SlackCapture.WaitForMessageAsync();

        await fixture.AskSlackbot(seeded, "<@UREFQD887> unsubscribe Standings");
        await fixture.SlackCapture.WaitForMessageAsync();

        var installation = await Repo.GetInstallation(seeded.TeamId);
        var channel = installation.GetChannel(channelId)!;
        Assert.False(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public async Task NoArgumentsPromptsForArguments()
    {
        var seeded = await fixture.SeedInstallation();

        await fixture.AskSlackbot(seeded, "<@UREFQD887> subscribe");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("You need to pass some arguments", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }
}
