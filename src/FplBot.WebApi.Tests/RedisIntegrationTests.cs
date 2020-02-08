using System;
using System.Linq;
using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.Extensions.Options;
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
                REDIS_SERVER = Environment.GetEnvironmentVariable("REDIS_SERVER"),
                REDIS_PASSWORD = Environment.GetEnvironmentVariable("REDIS_PASSWORD"),
                REDIS_USERNAME = Environment.GetEnvironmentVariable("REDIS_USERNAME")
            });
            string connStr = $"{opts.Value.REDIS_SERVER}, name={opts.Value.REDIS_USERNAME}, password={opts.Value.REDIS_PASSWORD}, allowAdmin=true";
            _helper.WriteLine(connStr);
            var multiplexer = ConnectionMultiplexer.Connect(connStr);
            _server = multiplexer.GetServer(opts.Value.REDIS_SERVER);
            _repo = new RedisSlackTeamRepository(multiplexer, opts);
        }

        [Fact]
        public async Task TestInsertAndFetchOne()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId1", AccessToken = "accessToken1"});

            var tokenFromRedis = await _repo.GetTokenByTeamId("teamId1");
            
            Assert.Equal("accessToken1", tokenFromRedis);
        }

        [Fact]
        public async Task TestInsertAndFetchAll()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId2", AccessToken = "accessToken2"});
            await _repo.Insert(new SlackTeam {TeamId = "teamId3", AccessToken = "accessToken3"});

            var tokensFromRedis = await _repo.GetTokens();

            Assert.Equal(2, tokensFromRedis.Count());
        }
        
        [Fact]
        public async Task TestGetTokenByTeamId()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId2", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#test"});

            var tokensFromRedis = await _repo.GetTokenByTeamId("teamId2");

            Assert.Equal("accessToken2", tokensFromRedis);
        }
        
        [Fact]
        public async Task TestInsertAndDelete()
        {
            await _repo.Insert(new SlackTeam {TeamId = "teamId2", AccessToken = "accessToken2", FplbotLeagueId = 123, FplBotSlackChannel = "#123"});
            await _repo.Insert(new SlackTeam {TeamId = "teamId3", AccessToken = "accessToken3", FplbotLeagueId = 234, FplBotSlackChannel = "#234"});


            await _repo.Delete("accessToken2");
            
            var tokensAfterDelete = await _repo.GetTokens();
            Assert.Single(tokensAfterDelete);
        }

        public void Dispose()
        {
            _server.FlushDatabase();
        }
    }
}