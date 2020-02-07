using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using StackExchange.Redis;

namespace FplBot.WebApi.Data
{
    public class RedisSlackTeamRepository : ISlackTeamRepository,ITokenStore
    {
        private readonly ConnectionMultiplexer _redis;
        private IDatabase _db;
        private string _server;


        public RedisSlackTeamRepository(ConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
            _server = redisOptions.Value.REDIS_SERVER;
        }
        
        public async Task Insert(SlackTeam slackTeam)
        {
            await _db.StringSetAsync(FromTeamIdToTeamKey(slackTeam.TeamId), slackTeam.AccessToken);
        }

        public async Task Delete(string teamId)
        {
            await _db.KeyDeleteAsync(FromTeamIdToTeamKey(teamId));
        }

        public async Task<IEnumerable<string>> GetTokens()
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
            var tokens =  await _db.StringGetAsync(keys: allTeamKeys.ToArray());
            return tokens.Select(t => t.ToString());
        }

        public async Task<string> GetTokenByTeamId(string teamId)
        {
            return await _db.StringGetAsync(FromTeamIdToTeamKey(teamId));
        }
        private static string FromTeamIdToTeamKey(string teamId)
        {
            return $"TeamId-{teamId}";
        }
    }
}