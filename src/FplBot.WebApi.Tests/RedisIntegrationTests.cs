using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.Extensions.Options;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.WebApi.Tests
{
    
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
            _repo = new RedisSlackTeamRepository(multiplexer, opts);
        }

        [Fact(Skip = "Exploratory test")]
        public async Task TestInsertAndFetchOne()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<string>{ "happy_hour", "sunsets"}});

            var tokenFromRedis = await _repo.GetTokenByTeamId("teamId1");
            
            Assert.Equal("accessToken1", tokenFromRedis);

            var team = await _repo.GetTeam("teamId1");
            
            Assert.Equal("teamId1", team.TeamId);
            Assert.Equal("teamName1", team.TeamName);
            Assert.Equal("accessToken1", team.AccessToken);
            Assert.Equal("#test", team.FplBotSlackChannel);
            Assert.Equal("happy_hour", team.Subscriptions.First());
            Assert.Equal("sunsets", team.Subscriptions.Last());
        }

        [Fact(Skip = "Exploratory test")]
        public async Task TestInsertAndFetchAll()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId2", TeamName = "teamName1", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<string>{ } });
            await _repo.Insert(new SlackTeam {TeamId = "teamId3", TeamName = "teamName2", AccessToken = "accessToken3", FplbotLeagueId = 123, FplBotSlackChannel = "#test", Subscriptions = new List<string>{ } });

            var tokensFromRedis = await _repo.GetTokens();

            Assert.Equal(2, tokensFromRedis.Count());
        }

        [Fact(Skip = "Exploratory test")]
        public async Task TestInsertAndDelete()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId2", TeamName = "teamName2", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<string>{ } });
            await _repo.Insert(new SlackTeam {TeamId = "teamId3", TeamName = "teamName3", AccessToken = "accessToken3", FplbotLeagueId = 234, FplBotSlackChannel = "#234", Subscriptions = new List<string>{ } });


            await _repo.Delete("accessToken2");
            
            var tokensAfterDelete = await _repo.GetTokens();
            Assert.Single(tokensAfterDelete);
        }
        
        [Fact(Skip = "Exploratory test")]
        public async Task UpdatesLeagueId()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<string>{ }});
            await _repo.UpdateLeagueId("teamId1", 456);
            var updated = await _repo.GetTeam("teamId1");

            Assert.Equal(456,updated.FplbotLeagueId);
        }
        
        [Fact(Skip = "Exploratory test")]
        public async Task Unsubscribe()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<string>{ "sub1", "sub2" }});
            await _repo.UpdateSubscriptions("teamId1", new List<string> { "sub2" });
            var updated = await _repo.GetTeam("teamId1");
            Assert.Equal(1,updated.Subscriptions.Count());
            Assert.DoesNotContain("sub1",updated.Subscriptions);
        }
        
        [Fact(Skip = "Exploratory test")]
        public async Task Subscribe()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", TeamName = "teamName1", AccessToken = "accessToken1", FplbotLeagueId = 123, FplBotSlackChannel = "#123", Subscriptions = new List<string>{ "sub1", "sub2" }});
            await _repo.UpdateSubscriptions("teamId1", new List<string> { "sub1","sub2", "sub3" });
            var updated = await _repo.GetTeam("teamId1");
            Assert.Equal(3,updated.Subscriptions.Count());
            Assert.Contains("sub3",updated.Subscriptions);
        }

        public void Dispose()
        {
            _server.FlushDatabase();
        }
    }
}