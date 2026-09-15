using FakeItEasy;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Services.WebApi.Slack.Handlers.Reactors;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.Tests.E2E.Slack;

[Collection("App")]
public class SlackbotNetInstallationBridgeTests(AppFixture fixture) : IAsyncLifetime
{
    private ISlackTeamRepository Repo => fixture.Services.GetRequiredService<ISlackTeamRepository>();
    private readonly TestPublishEndpoint _publishEndpoint = new();
    private SlackbotNetInstallationBridge _sut = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.FlushRedisAsync();
        _sut = new SlackbotNetInstallationBridge(Repo, _publishEndpoint, A.Fake<ILogger<SlackbotNetInstallationBridge>>());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Install_SavesBareInstallationAndPublishesAppInstalled()
    {
        await _sut.Install(new Workspace("T1", "Team One", "token1"));

        var stored = await Repo.GetInstallation("T1");
        Assert.Equal("Team One", stored.Name);
        Assert.Equal("token1", stored.Token);
        Assert.Empty(stored.ChannelSubscriptions);

        var published = Assert.Single(_publishEndpoint.PublishedMessages.Containing<AppInstalled>());
        var appInstalled = Assert.IsType<AppInstalled>(published.Message);
        Assert.Equal("T1", appInstalled.TeamId);
        Assert.Equal("Team One", appInstalled.TeamName);
        Assert.Equal(ChatPlatform.Slack, appInstalled.Platform);
    }

    [Fact]
    public async Task Install_OnAlreadyInstalledTeam_PreservesExistingChannelSubscriptions()
    {
        await _sut.Install(new Workspace("T1", "Team One", "token1"));
        await fixture.Subscribe("T1", "#fplbot", FplEvent.Standings);

        await _sut.Install(new Workspace("T1", "Team One", "token2"));

        var stored = await Repo.GetInstallation("T1");
        Assert.Equal("token2", stored.Token);
        var channel = Assert.Single(stored.ChannelSubscriptions);
        Assert.Equal("#fplbot", channel.ChannelId);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public async Task Uninstall_DelegatesToUninstallSlackWorkspace()
    {
        await _sut.Install(new Workspace("T1", "Team One", "token1"));

        await _sut.Uninstall("T1");

        Assert.Null(await Repo.FindInstallationByTeamId("T1"));
        Assert.Single(_publishEndpoint.PublishedMessages.Containing<AppUninstalled>());
    }

    [Fact]
    public async Task Uninstall_WhenAlreadyRemoved_DoesNotThrowOrPublish()
    {
        await _sut.Uninstall("T1");

        Assert.Empty(_publishEndpoint.PublishedMessages);
    }

    [Fact]
    public async Task GetChannelSubscription_ReturnsFollowedLeagueId_ForExistingChannel()
    {
        var installation = await fixture.SeedInstallation();
        var channel = installation.ChannelSubscriptions.Single();

        var result = await Repo.GetChannelSubscription(installation.Id, channel.ChannelId);

        Assert.NotNull(result);
        Assert.Equal(channel.FollowedLeagueId, result!.FollowedLeagueId);
    }

    [Fact]
    public async Task GetChannelSubscription_ReturnsNull_ForNonexistentChannel()
    {
        var installation = await fixture.SeedInstallation();

        var result = await Repo.GetChannelSubscription(installation.Id, "#does-not-exist");

        Assert.Null(result);
    }
}
