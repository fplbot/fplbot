using System.Net.Security;
using Fpl.Search.Data.Repositories;
using FplBot.Data.Discord;
using FplBot.Data.Slack;
using FplBot.Discord.Data;
using FplBot.VerifiedEntries.Data;
using FplBot.VerifiedEntries.Data.Models;
using FplBot.VerifiedEntries.Data.Repositories;
using FplBot.WebApi.Slack.Data;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;
using EventSubscription = FplBot.Data.Slack.EventSubscription;

namespace FplBot.WebApi.Tests;

public class RedisIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _helper;
    private readonly SlackTeamRepository _repo;
    private readonly LeagueIndexRedisBookmarkProvider _bookmarkProvider;
    private readonly IServer _server;
    private readonly VerifiedEntriesRepository _verifiedRepo;
    private readonly DiscordGuildRepository _guildRepo;
    private readonly DiscordGuildStore _guildStore;
    private readonly TokenStore _store;

    public RedisIntegrationTests(ITestOutputHelper helper)
    {
        _helper = helper;
        var opts = new OptionsWrapper<SlackRedisOptions>(new SlackRedisOptions
        {
            REDIS_URL = Environment.GetEnvironmentVariable("HEROKU_REDIS_COPPER_URL"),
        });

        var verifiedOpts = new OptionsWrapper<VerifiedRedisOptions>(new VerifiedRedisOptions
        {
            REDIS_URL = Environment.GetEnvironmentVariable("HEROKU_REDIS_COPPER_URL"),
        });


        var discordOpts = new OptionsWrapper<DiscordRedisOptions>(new DiscordRedisOptions
        {
            REDIS_URL = Environment.GetEnvironmentVariable("HEROKU_REDIS_COPPER_URL"),
        });

        var configurationOptions = new ConfigurationOptions
        {
            ClientName = opts.Value.GetRedisUsername,
            Password = opts.Value.GetRedisPassword,
            EndPoints = { opts.Value.GetRedisServerHostAndPort },
            AllowAdmin = true,
            Ssl = true,
            SslClientAuthenticationOptions = s => new SslClientAuthenticationOptions
            {
                TargetHost = opts.Value.GetHost,
                RemoteCertificateValidationCallback = (h, a, c, k) => true,
            }
        };

        var multiplexer = ConnectionMultiplexer.Connect(configurationOptions);
        _server = multiplexer.GetServer(opts.Value.GetRedisServerHostAndPort);
        _repo = new SlackTeamRepository(multiplexer, opts, new SimpleLogger(_helper));
        _store = new TokenStore(multiplexer, opts, new SimpleLogger(_helper));
        _bookmarkProvider = new LeagueIndexRedisBookmarkProvider(multiplexer, new SimpleLogger(_helper));
        _verifiedRepo = new VerifiedEntriesRepository(multiplexer, verifiedOpts, new SimpleLogger(_helper));
        _guildRepo = new DiscordGuildRepository(multiplexer, discordOpts, new SimpleLogger(_helper));
        _guildStore = new DiscordGuildStore(multiplexer, discordOpts, new SimpleLogger(_helper));
    }

    [Fact]
    public async Task TestInsertAndFetchOne()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription>{ EventSubscription.FixtureGoals, EventSubscription.Captains}});

        var tokenFromRedis = await _repo.GetTokenByTeamId("teamId1");

        Assert.Equal("accessToken1", tokenFromRedis);

        var team = await _repo.GetTeam("teamId1");

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
        await _store.Insert(new SlackTeam {TeamId = "teamId2", TeamName = "teamName1", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription> { } });
        await _store.Insert(new SlackTeam {TeamId = "teamId3", TeamName = "teamName2", AccessToken = "accessToken3", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription> { } });

        var tokensFromRedis = await _repo.GetTokens();

        Assert.Equal(2, tokensFromRedis.Count());
    }

    [Fact]
    public async Task TestInsertAndDelete()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId2", TeamName = "teamName2", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { } });
        await _store.Insert(new SlackTeam {TeamId = "teamId3", TeamName = "teamName3", AccessToken = "accessToken3", FplbotLeagueId = 234, FplBotSlackChannel = "#234", Subscriptions = new List<EventSubscription> { } });


        await _store.Delete("teamId2");

        var tokensAfterDelete = await _repo.GetTokens();
        Assert.Single(tokensAfterDelete);
    }

    [Fact]
    public async Task TestInsertAndDeleteCaseInvariant()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId2", TeamName = "teamName2", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { } });
        await _store.Insert(new SlackTeam {TeamId = "teamId3", TeamName = "teamName3", AccessToken = "accessToken3", FplbotLeagueId = 234, FplBotSlackChannel = "#234", Subscriptions = new List<EventSubscription> { } });


        var team = await _store.Delete("TEAMID2");

        var tokensAfterDelete = await _repo.GetTokens();
        Assert.Single(tokensAfterDelete);
        Assert.NotNull(team);
    }

    [Fact]
    public async Task UpdatesLeagueId()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { }});
        await _repo.UpdateLeagueId("teamId1", 456);
        var updated = await _repo.GetTeam("teamId1");

        Assert.Equal(456,updated.FplbotLeagueId);
    }

    [Fact]
    public async Task Unsubscribe()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards }});
        await _repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { EventSubscription.FixtureCards });
        var updated = await _repo.GetTeam("teamId1");

        Assert.Single(updated.Subscriptions);
        Assert.DoesNotContain(EventSubscription.FixtureAssists,updated.Subscriptions);
    }

    [Fact]
    public async Task Subscribe()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards } });
        await _repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards, EventSubscription.FixturePenaltyMisses });
        var updated = await _repo.GetTeam("teamId1");
        Assert.Equal(3,updated.Subscriptions.Count());
        Assert.Contains(EventSubscription.FixturePenaltyMisses, updated.Subscriptions);
    }

    [Fact]
    public async Task GetTeamWithNullSubs_ReturnsEmptySubsList()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = null});
        var team = await _repo.GetTeam("teamId1");
        Assert.Empty(team.Subscriptions);
    }

    [Fact]
    public async Task GetTeamWithNullSubs_UpdateToEmptyList_ReturnsEmptySubsList()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = null});
        await _repo.GetTeam("teamId1");
        await _repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { });
        var updated = await _repo.GetTeam("teamId1");
        Assert.Empty(updated.Subscriptions);
    }

    [Fact]
    public async Task GetTeamWithEmptySubs_ReturnsEmptySubsList()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { } });
        var updated = await _repo.GetTeam("teamId1");
        Assert.Empty(updated.Subscriptions);
    }

    [Fact]
    public async Task GetBookmarkTest()
    {
        await _bookmarkProvider.SetBookmark(1337);
        var bookmark = await _bookmarkProvider.GetBookmark();
        Assert.Equal(1337, bookmark);
    }

    [Fact]
    public async Task UpdatesDontCreateDuplicates()
    {
        VerifiedEntry verifiedEntry = SomeEntry() with { EntryStats = EntryStats()};

        await _verifiedRepo.Insert(verifiedEntry);
        var allVerifiedEntries = await _verifiedRepo.GetAllVerifiedEntries();
        Assert.Single(allVerifiedEntries);


        await _verifiedRepo.UpdateAllStats(verifiedEntry.EntryId, EntryStats() with {PointsThisGw = 100});
        var allVerifiedEntriesAfterUpdate = await _verifiedRepo.GetAllVerifiedEntries();
        Assert.Single(allVerifiedEntriesAfterUpdate);

    }

    [Fact]
    public async Task TestInsertWithOutFplData()
    {
        await _store.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1"});

        var tokenFromRedis = await _repo.GetTokenByTeamId("teamId1");

        Assert.Equal("accessToken1", tokenFromRedis);

        var team = await _repo.GetTeam("teamId1");

        Assert.Equal("teamId1", team.TeamId);
        Assert.Equal("teamName1", team.TeamName);
        Assert.Equal("accessToken1", team.AccessToken);
        Assert.Null(team.FplBotSlackChannel);
        Assert.Null(team.FplbotLeagueId);
        Assert.Empty(team.Subscriptions);
    }

    [Fact]
    public async Task Insert_Works()
    {
        await _guildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild1", "Channel1", null, new[] { Data.Discord.EventSubscription.All }));

        var guildSub = await _guildRepo.GetGuildSubscription("Guild1", "Channel1");
        Assert.NotNull(guildSub);
        Assert.NotEmpty(guildSub.Subscriptions);
    }

    [Fact]
    public async Task GetMany_Works()
    {
        await _guildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel1", null, new[] { Data.Discord.EventSubscription.All }));
        await _guildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel2", null, new[] { Data.Discord.EventSubscription.Standings }));

        foreach(var key in _server.Keys(pattern: "GuildSubs-Guild2-Channel-*")) {
            _helper.WriteLine(key);
        }

        var subs = await _guildRepo.GetAllGuildSubscriptions();


        Assert.Equal(2, subs.Count());

        var sub1 = await _guildRepo.GetGuildSubscription("Guild2", "Channel1");
        var sub2 = await _guildRepo.GetGuildSubscription("Guild2", "Channel2");

        Assert.Equal(Data.Discord.EventSubscription.All, sub1.Subscriptions.First());
        Assert.Equal(Data.Discord.EventSubscription.Standings, sub2.Subscriptions.First());

        var all = await _guildRepo.GetAllGuildSubscriptions();
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task Update_Works()
    {
        await _guildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel1", null, new[] { Data.Discord.EventSubscription.All }));
        await _guildRepo.InsertGuildSubscription(new GuildFplSubscription("Guild2", "Channel2", null, new[] { Data.Discord.EventSubscription.Standings }));

        var sub2 = await _guildRepo.GetGuildSubscription("Guild2", "Channel2");
        var update = sub2 with { Subscriptions = new[] { Data.Discord.EventSubscription.Lineups } };
        await _guildRepo.UpdateGuildSubscription(update);

        var sub2Updated = await _guildRepo.GetGuildSubscription("Guild2", "Channel2");
        Assert.Single(sub2Updated.Subscriptions);
        Assert.Equal(Data.Discord.EventSubscription.Lineups, sub2Updated.Subscriptions.First());

        var sub1NotUpdated = await _guildRepo.GetGuildSubscription("Guild2", "Channel1");
        Assert.Single(sub1NotUpdated.Subscriptions);
        Assert.Equal(Data.Discord.EventSubscription.All, sub1NotUpdated.Subscriptions.First());

    }

    private static VerifiedEntry SomeEntry()
    {
        return new VerifiedEntry(1, "fullname", "entryteamname", VerifiedEntryType.Footballer);
    }

    private static VerifiedEntryStats EntryStats()
    {
        return new VerifiedEntryStats(1, 2, 3, 50, "", "", "", 1);
    }

    public void Dispose()
    {
        _server.FlushDatabase();
    }
}
