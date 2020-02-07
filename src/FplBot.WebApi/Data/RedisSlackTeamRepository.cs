using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using StackExchange.Redis;

namespace FplBot.WebApi.Data
{
    public class RedisSlackTeamRepository : ISlackTeamRepository,ITokenStore, IFetchFplbotSetup
    {
        private readonly ConnectionMultiplexer _redis;
        private IDatabase _db;
        private string _server;

        private string _accessTokenField = "accessToken";
        private string _channelField = "fplchannel";
        private string _leagueField = "fplleagueId";

        public RedisSlackTeamRepository(ConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
            _server = redisOptions.Value.REDIS_SERVER;
        }
        
        public async Task Insert(SlackTeam slackTeam)
        {
            await _db.HashSetAsync(FromTeamIdToTeamKey(slackTeam.TeamId), new []
            {
                new HashEntry(_accessTokenField, slackTeam.AccessToken),
                new HashEntry(_channelField, slackTeam.FplBotSlackChannel), 
                new HashEntry(_leagueField, slackTeam.FplbotLeagueId)
            });
        }

        public async Task Delete(string teamId)
        {
            await _db.KeyDeleteAsync(FromTeamIdToTeamKey(teamId));
        }

        public async Task<IEnumerable<string>> GetTokens()
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
            var tokens = new List<string>();
            foreach (var key in allTeamKeys)
            {
                var token = await _db.HashGetAsync(key,_accessTokenField);
                tokens.Add(token);
            }
            return tokens.Select(t => t.ToString());
        }

        public async Task<string> GetTokenByTeamId(string teamId)
        {
            return await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), _accessTokenField);
        }
        private static string FromTeamIdToTeamKey(string teamId)
        {
            return $"TeamId-{teamId}";
        }

        public async Task<FplbotSetup> GetSetupByToken(string token)
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
            
            foreach (var key in allTeamKeys)
            {
                var fetchedTeamData = await _db.HashGetAsync(key, new RedisValue[] {_accessTokenField, _channelField, _leagueField});
                if (fetchedTeamData[0] == token)
                    return new FplbotSetup
                    {
                        Channel = fetchedTeamData[1],
                        LeagueId = int.Parse(fetchedTeamData[2])
                    };
            }

            return null;
        }
    }
}