using Fpl.Search.Data.Abstractions;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Tests.E2E;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Data;

[Collection("App")]
public class RedisIntegrationTests(AppFixture fixture) : IAsyncLifetime
{
    private ISlackTeamRepository Repo => fixture.Services.GetRequiredService<ISlackTeamRepository>();
    private IGuildRepository GuildRepo => fixture.Services.GetRequiredService<IGuildRepository>();
    private ILeagueIndexBookmarkProvider BookmarkProvider => fixture.Services.GetRequiredService<ILeagueIndexBookmarkProvider>();

    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static SlackInstallation Installation(string teamId, string teamName, string token, string? channel = null, long? leagueId = null, IEnumerable<FplEvent>? events = null)
    {
        var installation = SlackInstallation.Install(teamId, teamName, token);
        if (channel is not null)
        {
            if (leagueId.HasValue)
            {
                installation.Follow(channel, new ClassicLeagueId(leagueId.Value));
            }
            installation.Subscribe(channel, (events ?? []).ToArray());
        }
        return installation;
    }

    [Fact]
    public async Task TestInsertAndFetchOne()
    {
        await Repo.Save(Installation("teamId1", "teamName1", "accessToken1", "#test", 123, [FplEvent.FixtureGoals, FplEvent.Captains]));

        var installation = await Repo.GetInstallation("teamId1");

        Assert.Equal("teamId1", installation.TeamId);
        Assert.Equal("teamName1", installation.TeamName);
        Assert.Equal("accessToken1", installation.Token);
        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Equal("#test", channel.ChannelId);
        Assert.True(channel.IsSubscribedTo(FplEvent.FixtureGoals));
        Assert.True(channel.IsSubscribedTo(FplEvent.Captains));
    }

    [Fact]
    public async Task TestInsertAndFetchAll()
    {
        await Repo.Save(Installation("teamId2", "teamName1", "accessToken2", "#test", 123));
        await Repo.Save(Installation("teamId3", "teamName2", "accessToken3", "#test", 123));

        var teams = await Repo.GetAllInstallations();

        Assert.Equal(2, teams.Count());
    }

    [Fact]
    public async Task TestInsertAndDelete()
    {
        await Repo.Save(Installation("teamId2", "teamName2", "accessToken2", "#123", 123));
        await Repo.Save(Installation("teamId3", "teamName3", "accessToken3", "#234", 234));

        await Repo.DeleteByTeamId("teamId2");

        var teamsAfterDelete = await Repo.GetAllInstallations();
        Assert.Single(teamsAfterDelete);
    }

    [Fact]
    public async Task FindByTeamId_IsCaseInvariant()
    {
        await Repo.Save(Installation("teamId2", "teamName2", "accessToken2", "#123", 123));

        var found = await Repo.FindInstallationByTeamId("TEAMID2");

        Assert.NotNull(found);
        Assert.Equal("teamId2", found.TeamId);

        await Repo.DeleteByTeamId(found.TeamId);
        var teamsAfterDelete = await Repo.GetAllInstallations();
        Assert.Empty(teamsAfterDelete);
    }

    [Fact]
    public async Task UpdatesLeagueId()
    {
        await Repo.Save(Installation("teamId1", "teamName1", "accessToken1", "#123", 123));
        await Repo.UpdateLeagueId("teamId1", 456);
        var updated = await Repo.GetInstallation("teamId1");

        Assert.Equal(new ClassicLeagueId(456), Assert.Single(updated.ChannelSubscriptions).FollowedLeagueId);
    }

    [Fact]
    public async Task Unsubscribe()
    {
        await Repo.Save(Installation("teamId1", "teamName1", "accessToken1", "#123", 123, [FplEvent.FixtureAssists, FplEvent.FixtureCards]));
        await Repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { EventSubscription.FixtureCards });
        var updated = await Repo.GetInstallation("teamId1");

        var channel = Assert.Single(updated.ChannelSubscriptions);
        Assert.Equal(FplEvent.FixtureCards, Assert.Single(channel.Events.Current));
    }

    [Fact]
    public async Task Subscribe()
    {
        await Repo.Save(Installation("teamId1", "teamName1", "accessToken1", "#123", 123, [FplEvent.FixtureAssists, FplEvent.FixtureCards]));
        await Repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards, EventSubscription.FixturePenaltyMisses });
        var updated = await Repo.GetInstallation("teamId1");

        var channel = Assert.Single(updated.ChannelSubscriptions);
        Assert.Equal(3, channel.Events.Current.Count);
        Assert.True(channel.IsSubscribedTo(FplEvent.FixturePenaltyMisses));
    }

    [Fact]
    public async Task GetInstallationWithNoSubscriptions_ReturnsEmptySubscriptions()
    {
        await Repo.Save(Installation("teamId1", "teamName1", "accessToken1", "#123", 123));
        var installation = await Repo.GetInstallation("teamId1");
        var channel = Assert.Single(installation.ChannelSubscriptions);
        Assert.Empty(channel.Events.Current);
    }

    [Fact]
    public async Task GetInstallationWithNoSubscriptions_UpdateToEmptyList_ReturnsEmptySubscriptions()
    {
        await Repo.Save(Installation("teamId1", "teamName1", "accessToken1", "#123", 123));
        await Repo.GetInstallation("teamId1");
        await Repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { });
        var updated = await Repo.GetInstallation("teamId1");
        var channel = Assert.Single(updated.ChannelSubscriptions);
        Assert.Empty(channel.Events.Current);
    }

    [Fact]
    public async Task GetBookmarkTest()
    {
        await BookmarkProvider.SetBookmark(1337);
        var bookmark = await BookmarkProvider.GetBookmark();
        Assert.Equal(1337, bookmark);
    }

    [Fact]
    public async Task TestInsertWithOutFplData()
    {
        await Repo.Save(Installation("teamId1", "teamName1", "accessToken1"));

        var installation = await Repo.GetInstallation("teamId1");

        Assert.Equal("teamId1", installation.TeamId);
        Assert.Equal("teamName1", installation.TeamName);
        Assert.Equal("accessToken1", installation.Token);
        Assert.Empty(installation.ChannelSubscriptions);
    }

    [Fact]
    public async Task Insert_Works()
    {
        await GuildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel1", null, [EventSubscription.All
        ]));

        var guildSub = await GuildRepo.GetGuildSubscription("Guild1", "Channel1");
        Assert.NotNull(guildSub);
        Assert.NotEmpty(guildSub.Subscriptions);
    }

    [Fact]
    public async Task GetMany_Works()
    {
        await GuildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel1", null, [EventSubscription.All
        ]));
        await GuildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel2", null, [EventSubscription.Standings
        ]));

        var subs = await GuildRepo.GetAllGuildSubscriptions();

        Assert.Equal(2, subs.Count());

        var sub1 = await GuildRepo.GetGuildSubscription("Guild2", "Channel1");
        var sub2 = await GuildRepo.GetGuildSubscription("Guild2", "Channel2");

        Assert.Equal(EventSubscription.All, sub1.Subscriptions.First());
        Assert.Equal(EventSubscription.Standings, sub2.Subscriptions.First());
    }

    [Fact]
    public async Task Update_Works()
    {
        await GuildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel1", null, [EventSubscription.All
        ]));
        await GuildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel2", null, [EventSubscription.Standings
        ]));

        var sub2 = await GuildRepo.GetGuildSubscription("Guild2", "Channel2");
        var update = sub2 with { Subscriptions = [EventSubscription.Lineups] };
        await GuildRepo.UpdateGuildSubscription(update);

        var sub2Updated = await GuildRepo.GetGuildSubscription("Guild2", "Channel2");
        Assert.Single(sub2Updated.Subscriptions);
        Assert.Equal(EventSubscription.Lineups, sub2Updated.Subscriptions.First());

        var sub1NotUpdated = await GuildRepo.GetGuildSubscription("Guild2", "Channel1");
        Assert.Single(sub1NotUpdated.Subscriptions);
        Assert.Equal(EventSubscription.All, sub1NotUpdated.Subscriptions.First());
    }
}
