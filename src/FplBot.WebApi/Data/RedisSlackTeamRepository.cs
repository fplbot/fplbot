using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Abstractions.Hosting;
using StackExchange.Redis;

namespace FplBot.WebApi.Data
{
    public class RedisSlackTeamRepository : ISlackTeamRepository,ITokenStore
    {
        private readonly ConnectionMultiplexer _redis;
        private IDatabase _db;

        public RedisSlackTeamRepository(ConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
        }
        
        public async Task Insert(SlackTeam slackTeam)
        {
            await _db.StringSetAsync(slackTeam.TeamId, slackTeam.AccessToken);
            await _db.ListLeftPushAsync("InstalledTeams", FromTeamToString(slackTeam));
        }

        public async Task<IEnumerable<string>> GetTokens()
        {
            var allTeamTokens = await _db.ListRangeAsync("InstalledTeams");
            var tokens = new List<string>();
            foreach (var team in allTeamTokens)
            {
                tokens.Add(FromStringToTeam(team).AccessToken);
            }
            return tokens;
        }

        public async Task<string> GetTokenByTeamId(string teamId)
        {
            return await _db.StringGetAsync(teamId);
        }

        private static string FromTeamToString(SlackTeam slackTeam)
        {
            return $"{slackTeam.TeamId}:{slackTeam.AccessToken}";
        }
        
        private static SlackTeam FromStringToTeam(string teamString)
        {
            return new SlackTeam
            {
                TeamId = teamString.Split(":")[0],
                AccessToken = teamString.Split(":")[1]
            };
        }
    }
}