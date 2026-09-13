using Fpl.Search.Data.Abstractions;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
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

    [Fact]
    public async Task TestInsertAndFetchOne()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription>{ EventSubscription.FixtureGoals, EventSubscription.Captains}});

        var team = await Repo.GetTeam("teamId1");

        Assert.Equal("teamId1", team.TeamId);
        Assert.Equal("teamName1", team.TeamName);
        Assert.Equal("accessToken1", team.AccessToken);
        Assert.Equal("#test", team.FplBotSlackChannel);
        Assert.Equal(EventSubscription.FixtureGoals, team.Subscriptions.First());
        Assert.Equal(EventSubscription.Captains, team.Subscriptions.Last());
    }

    [Fact]
    public async Task TestInsertAndFetchAll()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId2", TeamName = "teamName1", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription> { } });
        await Repo.Save(new SlackTeam {TeamId = "teamId3", TeamName = "teamName2", AccessToken = "accessToken3", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription> { } });

        var teams = await Repo.GetAllTeams();

        Assert.Equal(2, teams.Count());
    }

    [Fact]
    public async Task TestInsertAndDelete()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId2", TeamName = "teamName2", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { } });
        await Repo.Save(new SlackTeam {TeamId = "teamId3", TeamName = "teamName3", AccessToken = "accessToken3", FplbotLeagueId = 234, FplBotSlackChannel = "#234", Subscriptions = new List<EventSubscription> { } });

        await Repo.DeleteByTeamId("teamId2");

        var teamsAfterDelete = await Repo.GetAllTeams();
        Assert.Single(teamsAfterDelete);
    }

    [Fact]
    public async Task FindByTeamId_IsCaseInvariant()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId2", TeamName = "teamName2", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { } });

        var found = await Repo.FindByTeamId("TEAMID2");

        Assert.NotNull(found);
        Assert.Equal("teamId2", found.TeamId);

        await Repo.DeleteByTeamId(found.TeamId!);
        var teamsAfterDelete = await Repo.GetAllTeams();
        Assert.Empty(teamsAfterDelete);
    }

    [Fact]
    public async Task UpdatesLeagueId()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { }});
        await Repo.UpdateLeagueId("teamId1", 456);
        var updated = await Repo.GetTeam("teamId1");

        Assert.Equal(456,updated.FplbotLeagueId);
    }

    [Fact]
    public async Task Unsubscribe()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards }});
        await Repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { EventSubscription.FixtureCards });
        var updated = await Repo.GetTeam("teamId1");

        Assert.Single(updated.Subscriptions);
        Assert.DoesNotContain(EventSubscription.FixtureAssists,updated.Subscriptions);
    }

    [Fact]
    public async Task Subscribe()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards } });
        await Repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards, EventSubscription.FixturePenaltyMisses });
        var updated = await Repo.GetTeam("teamId1");
        Assert.Equal(3,updated.Subscriptions.Count());
        Assert.Contains(EventSubscription.FixturePenaltyMisses, updated.Subscriptions);
    }

    [Fact]
    public async Task GetTeamWithNullSubs_ReturnsEmptySubsList()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = null!});
        var team = await Repo.GetTeam("teamId1");
        Assert.Empty(team.Subscriptions);
    }

    [Fact]
    public async Task GetTeamWithNullSubs_UpdateToEmptyList_ReturnsEmptySubsList()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = null!});
        await Repo.GetTeam("teamId1");
        await Repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { });
        var updated = await Repo.GetTeam("teamId1");
        Assert.Empty(updated.Subscriptions);
    }

    [Fact]
    public async Task GetTeamWithEmptySubs_ReturnsEmptySubsList()
    {
        await Repo.Save(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { } });
        var updated = await Repo.GetTeam("teamId1");
        Assert.Empty(updated.Subscriptions);
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
        await Repo.Save(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1"});

        var team = await Repo.GetTeam("teamId1");

        Assert.Equal("teamId1", team.TeamId);
        Assert.Equal("teamName1", team.TeamName);
        Assert.Equal("accessToken1", team.AccessToken);
        Assert.Null(team.FplBotSlackChannel);
        Assert.Null(team.FplbotLeagueId);
        Assert.Empty(team.Subscriptions);
    }

    [Fact]
    public async Task PendingRemoval_RoundTripsThroughRedis()
    {
        await Repo.Save(new SlackTeam { TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", PendingRemoval = true });

        var team = await Repo.GetTeam("teamId1");

        Assert.True(team.PendingRemoval);
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
