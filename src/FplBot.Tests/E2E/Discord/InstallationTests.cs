using Discord.Net.Endpoints.Hosting;
using FakeItEasy;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Services.WebApi.Discord.Handlers.Reactors;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordNetInstallationBridgeTests(AppFixture fixture) : IAsyncLifetime
{
    private IGuildRepository Repo => fixture.GuildRepo;
    private readonly TestPublishEndpoint _publishEndpoint = new();
    private DiscordNetInstallationBridge _sut = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.FlushRedisAsync();
        _sut = new DiscordNetInstallationBridge(Repo, _publishEndpoint, A.Fake<ILogger<DiscordNetInstallationBridge>>());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Install_SavesBareInstallationAndPublishesAppInstalled()
    {
        await _sut.Install(new Guild("G1", "Guild One"));

        var stored = await Repo.GetInstallation("G1");
        Assert.Equal("Guild One", stored.Name);
        Assert.Null(stored.Token);
        Assert.Empty(stored.ChannelSubscriptions);

        var published = Assert.Single(_publishEndpoint.PublishedMessages.Containing<AppInstalled>());
        var appInstalled = Assert.IsType<AppInstalled>(published.Message);
        Assert.Equal("G1", appInstalled.TeamId);
        Assert.Equal("Guild One", appInstalled.TeamName);
        Assert.Equal(ChatPlatform.Discord, appInstalled.Platform);
    }

    [Fact]
    public async Task Install_OnAlreadyInstalledGuild_PreservesExistingChannelSubscriptions()
    {
        await _sut.Install(new Guild("G1", "Guild One"));
        var installation = await Repo.GetInstallation("G1");
        installation.Subscribe("C1", [FplEvent.Standings]);
        await Repo.Save(installation);

        await _sut.Install(new Guild("G1", "Guild One Renamed"));

        var stored = await Repo.GetInstallation("G1");
        Assert.Equal("Guild One Renamed", stored.Name);
        var channel = Assert.Single(stored.ChannelSubscriptions);
        Assert.Equal("C1", channel.ChannelId);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public async Task Uninstall_DeletesInstallationAndPublishesAppUninstalled()
    {
        await _sut.Install(new Guild("G1", "Guild One"));

        await _sut.Uninstall("G1");

        Assert.Null(await Repo.FindInstallationByTeamId("G1"));
        Assert.Single(_publishEndpoint.PublishedMessages.Containing<AppUninstalled>());
    }

    [Fact]
    public async Task Uninstall_WhenAlreadyRemoved_DoesNotThrowOrPublish()
    {
        await _sut.Uninstall("G1");

        Assert.Empty(_publishEndpoint.PublishedMessages);
    }

    [Fact]
    public async Task GetChannelSubscription_ReturnsFollowedLeagueId_ForExistingChannel()
    {
        var installation = await fixture.SeedGuildInstallation(leagueId: 12345);
        var channel = installation.ChannelSubscriptions.Single();

        var result = await Repo.GetChannelSubscription(installation.Id, channel.ChannelId);

        Assert.NotNull(result);
        Assert.Equal(channel.FollowedLeagueId, result!.FollowedLeagueId);
    }

    [Fact]
    public async Task GetChannelSubscription_ReturnsNull_ForNonexistentChannel()
    {
        var installation = await fixture.SeedGuildInstallation(leagueId: 12345);

        var result = await Repo.GetChannelSubscription(installation.Id, "does-not-exist");

        Assert.Null(result);
    }
}
