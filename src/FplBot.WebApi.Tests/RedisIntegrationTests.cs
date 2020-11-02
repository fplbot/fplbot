using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.WebApi.Tests
{
    public class SimpleLogger : ILogger<RedisSlackTeamRepository>
    {
        private readonly ITestOutputHelper _helper;

        public SimpleLogger(ITestOutputHelper helper)
        {
            _helper = helper;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _helper.WriteLine(formatter(state, exception));
        }
    }
    
    public class RedisIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _helper;
        private RedisSlackTeamRepository _repo;
        private IServer _server;

        public RedisIntegrationTests(ITestOutputHelper helper)
        {
            _helper = helper;
            var opts = new OptionsWrapper<RedisOptions>(new RedisOptions
            {
                REDIS_URL = Environment.GetEnvironmentVariable("HEROKU_REDIS_COPPER_URL"),
            });

            var configurationOptions = new ConfigurationOptions
            {
                ClientName = opts.Value.GetRedisUsername,
                Password = opts.Value.GetRedisPassword,
                EndPoints = { opts.Value.GetRedisServerHostAndPort },
                AllowAdmin = true
            };
            
            var multiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            _server = multiplexer.GetServer(opts.Value.GetRedisServerHostAndPort);
            _repo = new RedisSlackTeamRepository(multiplexer, opts, new SimpleLogger(_helper));
        }

        [Fact]
        public async Task TestInsertAndFetchOne()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription>{ EventSubscription.FixtureGoals, EventSubscription.Captains}});

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
            await _repo.Insert(new SlackTeam {TeamId = "teamId2", TeamName = "teamName1", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription> { } });
            await _repo.Insert(new SlackTeam {TeamId = "teamId3", TeamName = "teamName2", AccessToken = "accessToken3", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<EventSubscription> { } });

            var tokensFromRedis = await _repo.GetTokens();

            Assert.Equal(2, tokensFromRedis.Count());
        }

        [Fact]
        public async Task TestInsertAndDelete()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId2", TeamName = "teamName2", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { } });
            await _repo.Insert(new SlackTeam {TeamId = "teamId3", TeamName = "teamName3", AccessToken = "accessToken3", FplbotLeagueId = 234, FplBotSlackChannel = "#234", Subscriptions = new List<EventSubscription> { } });


            await _repo.Delete("accessToken2");
            
            var tokensAfterDelete = await _repo.GetTokens();
            Assert.Single(tokensAfterDelete);
        }
        
        [Fact]
        public async Task UpdatesLeagueId()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { }});
            await _repo.UpdateLeagueId("teamId1", 456);
            var updated = await _repo.GetTeam("teamId1");

            Assert.Equal(456,updated.FplbotLeagueId);
        }
        
        [Fact]
        public async Task Unsubscribe()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards }});
            await _repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { EventSubscription.FixtureCards });
            var updated = await _repo.GetTeam("teamId1");

            Assert.Single(updated.Subscriptions);
            Assert.DoesNotContain(EventSubscription.FixtureAssists,updated.Subscriptions);
        }
        
        [Fact]
        public async Task Subscribe()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards } });
            await _repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { EventSubscription.FixtureAssists, EventSubscription.FixtureCards, EventSubscription.FixturePenaltyMisses });
            var updated = await _repo.GetTeam("teamId1");
            Assert.Equal(3,updated.Subscriptions.Count());
            Assert.Contains(EventSubscription.FixturePenaltyMisses, updated.Subscriptions);
        }
        
        [Fact]
        public async Task GetTeamWithNullSubs_ReturnsEmptySubsList()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = null});
            var team = await _repo.GetTeam("teamId1"); 
            Assert.Empty(team.Subscriptions);
        }
        
        [Fact]
        public async Task GetTeamWithNullSubs_UpdateToEmptyList_ReturnsEmptySubsList()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = null});
            await _repo.GetTeam("teamId1"); 
            await _repo.UpdateSubscriptions("teamId1", new List<EventSubscription> { });
            var updated = await _repo.GetTeam("teamId1");
            Assert.Empty(updated.Subscriptions);
        }

        [Fact]
        public async Task GetTeamWithEmptySubs_ReturnsEmptySubsList()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<EventSubscription> { } });
            var updated = await _repo.GetTeam("teamId1");
            Assert.Empty(updated.Subscriptions);
        }

        public void Dispose()
        {
            _server.FlushDatabase();
        }
    }
}