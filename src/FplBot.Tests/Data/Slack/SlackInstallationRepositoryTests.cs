using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Tests.E2E;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Data.Slack;

[Collection("App")]
public class SlackInstallationRepositoryTests(AppFixture fixture) : IAsyncLifetime
{
    private ISlackTeamRepository Repo => fixture.Services.GetRequiredService<ISlackTeamRepository>();

    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SaveThenFetch_NoChannelFollowed_ReturnsActiveInstallationWithNoSubscriptions()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");

        await Repo.Save(installation);
        var fetched = await Repo.GetInstallation("T1");

        Assert.Equal("T1", fetched.TeamId);
        Assert.Equal("Team One", fetched.TeamName);
        Assert.Equal("token1", fetched.Token);
        Assert.True(fetched.IsActive);
        Assert.Empty(fetched.ChannelSubscriptions);
    }

    [Fact]
    public async Task SaveThenFetch_ChannelFollowedAndSubscribed_ReturnsMatchingChannelSubscription()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        installation.Follow("#fpl", new ClassicLeagueId(42));
        installation.Subscribe("#fpl", [FplEvent.Standings, FplEvent.Deadlines]);

        await Repo.Save(installation);
        var fetched = await Repo.GetInstallation("T1");

        var channel = Assert.Single(fetched.ChannelSubscriptions);
        Assert.Equal("#fpl", channel.ChannelId);
        Assert.Equal(new ClassicLeagueId(42), channel.FollowedLeagueId);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
        Assert.False(channel.IsSubscribedTo(FplEvent.Captains));
    }

    [Fact]
    public async Task SaveThenFetch_SubscribedToAll_IsSubscribedToEverything()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        installation.Follow("#fpl", new ClassicLeagueId(1));
        installation.Subscribe("#fpl", [FplEvent.All]);

        await Repo.Save(installation);
        var fetched = await Repo.GetInstallation("T1");

        var channel = Assert.Single(fetched.ChannelSubscriptions);
        Assert.True(channel.IsSubscribedTo(FplEvent.Captains));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public async Task SaveThenFetch_MarkedForRemoval_ReturnsInactiveInstallation()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        installation.MarkForRemoval();

        await Repo.Save(installation);
        var fetched = await Repo.GetInstallation("T1");

        Assert.True(fetched.PendingRemoval);
        Assert.False(fetched.IsActive);
    }
}
