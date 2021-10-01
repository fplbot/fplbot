using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FplBot.Core.Data.Abstractions;
using FplBot.Core.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Slackbot.Net.Abstractions.Hosting;
using StackExchange.Redis;

namespace FplBot.Core.Data.Repositories.Redis
{
    public class SlackTeamRepository : ISlackTeamRepository, ITokenStore
    {
        private readonly ILogger<SlackTeamRepository> _logger;

        private readonly ConnectionMultiplexer _redis;
        private IDatabase _db;
        private string _server;

        private string _accessTokenField = "accessToken";
        private string _channelField = "fplchannel";
        private string _leagueField = "fplleagueId";
        private string _teamNameField = "teamName";
        private string _teamIdField = "teamId";
        private string _subscriptionsField = "subscriptions";

        public SlackTeamRepository(ConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions, ILogger<SlackTeamRepository> logger)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
            _server = redisOptions.Value.GetRedisServerHostAndPort;
            _logger = logger;
        }

        public async Task Insert(Workspace workspace)
        {
            await Insert(new SlackTeam
            {
                TeamId = workspace.TeamId,
                TeamName = workspace.TeamName,
                AccessToken = workspace.Token
            });
        }

        public async Task Insert(SlackTeam slackTeam)
        {
            var hashEntries = new List<HashEntry>
            {
                new HashEntry(_accessTokenField, slackTeam.AccessToken),
                new HashEntry(_teamNameField, slackTeam.TeamName),
                new HashEntry(_teamIdField, slackTeam.TeamId)
            };

            if (!string.IsNullOrEmpty(slackTeam.FplBotSlackChannel))
            {
                hashEntries.Add(new HashEntry(_channelField, slackTeam.FplBotSlackChannel));
            }

            if (slackTeam.FplbotLeagueId > 0)
            {
                hashEntries.Add(new HashEntry(_leagueField, slackTeam.FplbotLeagueId));
            }

            if (slackTeam.Subscriptions != null)
            {
                var hashEntry = new HashEntry(_subscriptionsField, string.Join(" ", slackTeam.Subscriptions));
                hashEntries.Add(hashEntry);
            }

            await _db.HashSetAsync(FromTeamIdToTeamKey(slackTeam.TeamId), hashEntries.ToArray());
        }

        public async Task<Workspace> Delete(string teamId)
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));

            foreach (var key in allTeamKeys)
            {
                var fetchedTeamData = await _db.HashGetAsync(key, new RedisValue[] {_teamIdField, _teamNameField, _accessTokenField});
                if (fetchedTeamData[0] == teamId)
                {
                    var workspace = new Workspace(TeamId: fetchedTeamData[0], TeamName: fetchedTeamData[1], Token: fetchedTeamData[2]);
                    await _db.KeyDeleteAsync(key);
                    return workspace;
                }

            }

            return null;
        }

        public async Task<SlackTeam> GetTeam(string teamId)
        {
            var fetchedTeamData = await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), new RedisValue[] {_accessTokenField, _channelField, _leagueField, _teamNameField, _subscriptionsField});

            var team = new SlackTeam
            {
                AccessToken = fetchedTeamData[0],
                TeamName = fetchedTeamData[3],
                TeamId = teamId
            };

            if (fetchedTeamData[1].HasValue)
            {
                team.FplBotSlackChannel = fetchedTeamData[1];
            }

            if (fetchedTeamData[2].HasValue)
            {
                team.FplbotLeagueId = int.Parse(fetchedTeamData[2]);
            }

            var subs = GetSubscriptions(teamId, fetchedTeamData[4]);

            team.Subscriptions = subs;

            return team;
        }

        private List<EventSubscription> GetSubscriptions(string teamId, RedisValue fetchedTeamData)
        {

            if (!fetchedTeamData.HasValue)
            {
                return new List<EventSubscription>();
            }

            (var subs, var unableToParse) = fetchedTeamData.ToString().ParseSubscriptionString(delimiter: " ");

            if (unableToParse.Any())
            {
                _logger.LogError("Unable to parse events for team {team}: {unableToParse}", teamId, string.Join(", ", unableToParse));
            }

            return subs.ToList<EventSubscription>();
        }

        public async Task UpdateLeagueId(string teamId, long newLeagueId)
        {
            if(string.IsNullOrEmpty(teamId))
                throw new ArgumentNullException(nameof(teamId));

            if(newLeagueId == 0)
                throw new ArgumentNullException(nameof(newLeagueId));

            await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), new [] { new HashEntry(_leagueField, newLeagueId) });
        }

        public async Task UpdateChannel(string teamId, string newChannel)
        {
            if(string.IsNullOrEmpty(teamId))
                throw new ArgumentNullException(nameof(teamId));

            if(string.IsNullOrEmpty(newChannel))
                throw new ArgumentNullException(nameof(newChannel));

            await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), new [] { new HashEntry(_channelField, newChannel) });
        }

        public async Task DeleteByTeamId(string teamId)
        {
            if(string.IsNullOrEmpty(teamId))
                throw new ArgumentNullException(nameof(teamId));

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
                var teamId = FromKeyToTeamId(key);

                var fetchedTeamData = await _db.HashGetAsync(key, new RedisValue[] {_accessTokenField, _channelField, _leagueField, _teamNameField, _subscriptionsField});

                var slackTeam = new SlackTeam
                {
                    AccessToken = fetchedTeamData[0],
                    TeamName = fetchedTeamData[3],
                    TeamId = teamId
                };

                if (fetchedTeamData[1].HasValue)
                {
                    slackTeam.FplBotSlackChannel = fetchedTeamData[1];
                }

                if (fetchedTeamData[2].HasValue)
                {
                    slackTeam.FplbotLeagueId = int.Parse(fetchedTeamData[2]);
                }

                var subs = GetSubscriptions(teamId, fetchedTeamData[4]);

                slackTeam.Subscriptions = subs;
                teams.Add(slackTeam);
            }

            return teams;
        }

        public async Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions)
        {
            if(string.IsNullOrEmpty(teamId))
                throw new ArgumentNullException(nameof(teamId));

            await _db.HashSetAsync(FromTeamIdToTeamKey(teamId), new [] { new HashEntry(_subscriptionsField, string.Join(" ", subscriptions)) });
        }
    }
}