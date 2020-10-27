using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FplBot.WebApi.Data
{
    public class RedisSlackTeamRepository : ISlackTeamRepository, ITokenStore
    {
        private readonly ConnectionMultiplexer _redis;
        private IDatabase _db;
        private string _server;

        private string _accessTokenField = "accessToken";
        private string _channelField = "fplchannel";
        private string _leagueField = "fplleagueId";
        private string _teamNameField = "teamName";
        private string _teamIdField = "teamId";
        private string _subscriptionsField = "subscriptions";

        public RedisSlackTeamRepository(ConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
            _server = redisOptions.Value.GetRedisServerHostAndPort;
        }
        
        public async Task Insert(SlackTeam slackTeam)
        {
            await _db.HashSetAsync(FromTeamIdToTeamKey(slackTeam.TeamId), new []
            {
                new HashEntry(_accessTokenField, slackTeam.AccessToken),
                new HashEntry(_channelField, slackTeam.FplBotSlackChannel), 
                new HashEntry(_leagueField, slackTeam.FplbotLeagueId),
                new HashEntry(_teamNameField, slackTeam.TeamName),
                new HashEntry(_teamIdField, slackTeam.TeamId),
                new HashEntry(_subscriptionsField, string.Join(" ", slackTeam.Subscriptions)) 
            });
        }

        public async Task Delete(string token)
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
            
            foreach (var key in allTeamKeys)
            {
                var fetchedTeamData = await _db.HashGetAsync(key, new RedisValue[] {_accessTokenField, _channelField, _leagueField, _teamIdField});
                if (fetchedTeamData[0] == token)
                    await _db.KeyDeleteAsync(key);
            }
        }

        public async Task<SlackTeam> GetTeam(string teamId)
        {
            var fetchedTeamData = await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), new RedisValue[] {_accessTokenField, _channelField, _leagueField, _teamNameField, _subscriptionsField});
            
            var team = new SlackTeam
            {
                AccessToken = fetchedTeamData[0],
                FplBotSlackChannel = fetchedTeamData[1],
                FplbotLeagueId = int.Parse(fetchedTeamData[2]),
                TeamName = fetchedTeamData[3],
                TeamId = teamId
            };

            var subs = !fetchedTeamData[4].HasValue
                ? new List<EventSubscription> {EventSubscription.All}
                : fetchedTeamData[4].ToString().ParseSubscriptionString(delimiter: " ").ToList();

            team.Subscriptions = subs;

            return team;
        }

        public async Task UpdateLeagueId(string teamId, long newLeagueId)
        {
            await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), new [] { new HashEntry(_leagueField, newLeagueId) });
        }
        
        public async Task UpdateChannel(string teamId, string newChannel)
        {
            await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), new [] { new HashEntry(_channelField, newChannel) });
        }

        public async Task DeleteByTeamId(string teamId)
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
        
        private static string FromKeyToTeamId(string key)
        {
            return key.Split('-')[1];
        }

        public async Task<IEnumerable<SlackTeam>> GetAllTeams()
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
            var teams = new List<SlackTeam>();
            foreach (var key in allTeamKeys)
            {
                var fetchedTeamData = await _db.HashGetAsync(key, new RedisValue[] {_accessTokenField, _channelField, _leagueField, _teamNameField, _subscriptionsField});
                var slackTeam = new SlackTeam
                {
                    AccessToken = fetchedTeamData[0],
                    FplBotSlackChannel = fetchedTeamData[1],
                    FplbotLeagueId = int.Parse(fetchedTeamData[2]),
                    TeamName = fetchedTeamData[3],
                    TeamId = FromKeyToTeamId(key)
                };
                var subs = !fetchedTeamData[4].HasValue
                    ? new List<EventSubscription> {EventSubscription.All}
                    : fetchedTeamData[4].ToString().ParseSubscriptionString(delimiter: " ").ToList();
                slackTeam.Subscriptions = subs;
                teams.Add(slackTeam);
            }

            return teams;
        }

        public async Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions)
        {
            await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), new [] { new HashEntry(_subscriptionsField, string.Join(" ", subscriptions)) });
        }
    }
}