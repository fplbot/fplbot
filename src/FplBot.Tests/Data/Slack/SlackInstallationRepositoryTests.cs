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

    [Fact]
    public async Task GetChannelSubscriptions_NothingSaved_ReturnsEmpty()
    {
        var result = await Repo.GetChannelSubscriptions("V2-EMPTY");

        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveChannelSubscription_ThenFetch_ReturnsMatchingChannel()
    {
        var channel = SlackChannelSubscription.FromStorage("#fpl", new ClassicLeagueId(42), [FplEvent.Standings, FplEvent.Deadlines]);

        await Repo.SaveChannelSubscription("V2-1", channel);
        var fetched = await Repo.GetChannelSubscriptions("V2-1");

        var single = Assert.Single(fetched);
        Assert.Equal("#fpl", single.ChannelId);
        Assert.Equal(new ClassicLeagueId(42), single.FollowedLeagueId);
        Assert.True(single.IsSubscribedTo(FplEvent.Standings));
        Assert.True(single.IsSubscribedTo(FplEvent.Deadlines));
        Assert.False(single.IsSubscribedTo(FplEvent.Captains));
    }

    [Fact]
    public async Task SaveChannelSubscription_MultipleChannelsSameTeam_ReturnsAllChannels()
    {
        var teamId = "V2-MULTI";
        await Repo.SaveChannelSubscription(teamId, SlackChannelSubscription.FromStorage("#fpl-one", new ClassicLeagueId(1), [FplEvent.Standings]));
        await Repo.SaveChannelSubscription(teamId, SlackChannelSubscription.FromStorage("#fpl-two", new ClassicLeagueId(2), [FplEvent.Deadlines]));

        var fetched = (await Repo.GetChannelSubscriptions(teamId)).ToList();

        Assert.Equal(2, fetched.Count);
        Assert.Contains(fetched, c => c.ChannelId == "#fpl-one" && c.FollowedLeagueId == new ClassicLeagueId(1));
        Assert.Contains(fetched, c => c.ChannelId == "#fpl-two" && c.FollowedLeagueId == new ClassicLeagueId(2));
    }

    [Fact]
    public async Task SaveChannelSubscription_SameChannelTwice_OverwritesRatherThanDuplicates()
    {
        var teamId = "V2-OVERWRITE";
        await Repo.SaveChannelSubscription(teamId, SlackChannelSubscription.FromStorage("#fpl", new ClassicLeagueId(1), [FplEvent.Standings]));
        await Repo.SaveChannelSubscription(teamId, SlackChannelSubscription.FromStorage("#fpl", new ClassicLeagueId(2), [FplEvent.Deadlines]));

        var fetched = await Repo.GetChannelSubscriptions(teamId);

        var single = Assert.Single(fetched);
        Assert.Equal(new ClassicLeagueId(2), single.FollowedLeagueId);
        Assert.True(single.IsSubscribedTo(FplEvent.Deadlines));
        Assert.False(single.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public async Task SaveChannelSubscription_DifferentTeams_AreIsolated()
    {
        await Repo.SaveChannelSubscription("V2-TEAM-A", SlackChannelSubscription.FromStorage("#fpl", new ClassicLeagueId(1), [FplEvent.Standings]));
        await Repo.SaveChannelSubscription("V2-TEAM-B", SlackChannelSubscription.FromStorage("#fpl", new ClassicLeagueId(2), [FplEvent.Deadlines]));

        var teamA = await Repo.GetChannelSubscriptions("V2-TEAM-A");
        var teamB = await Repo.GetChannelSubscriptions("V2-TEAM-B");

        Assert.Equal(new ClassicLeagueId(1), Assert.Single(teamA).FollowedLeagueId);
        Assert.Equal(new ClassicLeagueId(2), Assert.Single(teamB).FollowedLeagueId);
    }
}
