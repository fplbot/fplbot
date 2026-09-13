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
        installation.Subscribe("#fpl", [FplEvent.Standings, FplEvent.Deadlines]);
        installation.Follow("#fpl", new ClassicLeagueId(42));

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
        installation.Subscribe("#fpl", [FplEvent.All]);
        installation.Follow("#fpl", new ClassicLeagueId(1));

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
    public async Task Follow_FreshChannel_SeedsAllEvents()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        installation.Follow("#fpl", new ClassicLeagueId(1));

        await Repo.Save(installation);
        var fetched = await Repo.GetInstallation("T1");

        var channel = Assert.Single(fetched.ChannelSubscriptions);
        Assert.True(channel.IsSubscribedTo(FplEvent.Captains));
        Assert.True(channel.IsSubscribedTo(FplEvent.Deadlines));
    }

    [Fact]
    public async Task Follow_ChannelAlreadySubscribed_DoesNotClobberExistingSubscriptions()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        installation.Subscribe("#fpl", [FplEvent.Standings]);
        installation.Follow("#fpl", new ClassicLeagueId(1));

        await Repo.Save(installation);
        var fetched = await Repo.GetInstallation("T1");

        var channel = Assert.Single(fetched.ChannelSubscriptions);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.False(channel.IsSubscribedTo(FplEvent.Captains));
    }

    [Fact]
    public async Task Follow_SameChannelAgainWithDifferentLeague_OnlyUpdatesLeagueId()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        installation.Subscribe("#fpl", [FplEvent.Standings]);
        installation.Follow("#fpl", new ClassicLeagueId(1));
        await Repo.Save(installation);

        var fetched = await Repo.GetInstallation("T1");
        fetched.Follow("#fpl", new ClassicLeagueId(2));
        await Repo.Save(fetched);

        var updated = await Repo.GetInstallation("T1");
        var channel = Assert.Single(updated.ChannelSubscriptions);
        Assert.Equal(new ClassicLeagueId(2), channel.FollowedLeagueId);
        Assert.True(channel.IsSubscribedTo(FplEvent.Standings));
        Assert.False(channel.IsSubscribedTo(FplEvent.Captains));
    }

    [Fact]
    public async Task Save_ThenGetChannelSubscriptions_ReturnsMatchingChannel()
    {
        var installation = SlackInstallation.Install("V2-1", "Team", "token1");
        installation.Subscribe("#fpl", [FplEvent.Standings, FplEvent.Deadlines]);
        installation.Follow("#fpl", new ClassicLeagueId(42));

        await Repo.Save(installation);
        var fetched = await Repo.GetChannelSubscriptions("V2-1");

        var single = Assert.Single(fetched);
        Assert.Equal("#fpl", single.ChannelId);
        Assert.Equal(new ClassicLeagueId(42), single.FollowedLeagueId);
        Assert.True(single.IsSubscribedTo(FplEvent.Standings));
        Assert.True(single.IsSubscribedTo(FplEvent.Deadlines));
        Assert.False(single.IsSubscribedTo(FplEvent.Captains));
    }

    [Fact]
    public async Task Save_MultipleChannelsSameTeam_ReturnsAllChannels()
    {
        var teamId = "V2-MULTI";
        var installation = SlackInstallation.Install(teamId, "Team", "token1");
        installation.Subscribe("#fpl-one", [FplEvent.Standings]);
        installation.Follow("#fpl-one", new ClassicLeagueId(1));
        installation.Subscribe("#fpl-two", [FplEvent.Deadlines]);
        installation.Follow("#fpl-two", new ClassicLeagueId(2));

        await Repo.Save(installation);
        var fetched = (await Repo.GetChannelSubscriptions(teamId)).ToList();

        Assert.Equal(2, fetched.Count);
        Assert.Contains(fetched, c => c.ChannelId == "#fpl-one" && c.FollowedLeagueId == new ClassicLeagueId(1));
        Assert.Contains(fetched, c => c.ChannelId == "#fpl-two" && c.FollowedLeagueId == new ClassicLeagueId(2));
    }

    [Fact]
    public async Task Save_SameChannelTwice_OverwritesRatherThanDuplicates()
    {
        var teamId = "V2-OVERWRITE";
        var installation = SlackInstallation.Install(teamId, "Team", "token1");
        installation.Subscribe("#fpl", [FplEvent.Standings]);
        installation.Follow("#fpl", new ClassicLeagueId(1));
        await Repo.Save(installation);

        var reloaded = await Repo.GetInstallation(teamId);
        reloaded.Unsubscribe("#fpl", [FplEvent.Standings]);
        reloaded.Subscribe("#fpl", [FplEvent.Deadlines]);
        reloaded.Follow("#fpl", new ClassicLeagueId(2));
        await Repo.Save(reloaded);

        var fetched = await Repo.GetChannelSubscriptions(teamId);

        var single = Assert.Single(fetched);
        Assert.Equal(new ClassicLeagueId(2), single.FollowedLeagueId);
        Assert.True(single.IsSubscribedTo(FplEvent.Deadlines));
        Assert.False(single.IsSubscribedTo(FplEvent.Standings));
    }

    [Fact]
    public async Task Save_DifferentTeams_AreIsolated()
    {
        var teamA = SlackInstallation.Install("V2-TEAM-A", "Team A", "token1");
        teamA.Subscribe("#fpl", [FplEvent.Standings]);
        teamA.Follow("#fpl", new ClassicLeagueId(1));
        await Repo.Save(teamA);

        var teamB = SlackInstallation.Install("V2-TEAM-B", "Team B", "token2");
        teamB.Subscribe("#fpl", [FplEvent.Deadlines]);
        teamB.Follow("#fpl", new ClassicLeagueId(2));
        await Repo.Save(teamB);

        var fetchedA = await Repo.GetChannelSubscriptions("V2-TEAM-A");
        var fetchedB = await Repo.GetChannelSubscriptions("V2-TEAM-B");

        Assert.Equal(new ClassicLeagueId(1), Assert.Single(fetchedA).FollowedLeagueId);
        Assert.Equal(new ClassicLeagueId(2), Assert.Single(fetchedB).FollowedLeagueId);
    }

    [Fact]
    public async Task Save_TeamIdIsPrefixOfAnotherTeamId_DoesNotLeakTheOtherTeamsChannels()
    {
        // Regression test: a team whose id is a string-prefix of another team's id (e.g.
        // "DEV-SLACK" vs "DEV-SLACK-2") must not see the other team's channels.
        var pfx = SlackInstallation.Install("PFX", "Team", "token1");
        pfx.Subscribe("#fpl", [FplEvent.Standings]);
        pfx.Follow("#fpl", new ClassicLeagueId(1));
        await Repo.Save(pfx);

        var pfx2 = SlackInstallation.Install("PFX-2", "Team", "token2");
        pfx2.Subscribe("#other", [FplEvent.Deadlines]);
        pfx2.Follow("#other", new ClassicLeagueId(2));
        await Repo.Save(pfx2);

        var pfxBare = SlackInstallation.Install("PFX-BARE", "Team", "token3");
        pfxBare.Subscribe("#bare", [FplEvent.Captains]);
        pfxBare.Follow("#bare", new ClassicLeagueId(3));
        await Repo.Save(pfxBare);

        var fetched = await Repo.GetChannelSubscriptions("PFX");

        var single = Assert.Single(fetched);
        Assert.Equal("#fpl", single.ChannelId);
        Assert.Equal(new ClassicLeagueId(1), single.FollowedLeagueId);
    }
}
