using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Discord.Data
{
    public class DiscordGuildStore : IGuildRepository
    {
        private readonly RedisValue _nameField = "name";
        private readonly RedisValue _guildIdField = "guildid";
        private readonly RedisValue _channelIdField = "channelid";
        private readonly RedisValue _leagueIdField = "leagueid";
        private readonly RedisValue _subscriptionsField = "subs";

        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly string _server;
        private readonly ILogger<DiscordGuildStore> _logger;

        public DiscordGuildStore(IConnectionMultiplexer redis, IOptions<DiscordRedisOptions> redisOptions, ILogger<DiscordGuildStore> logger)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
            _server = redisOptions.Value.GetRedisServerHostAndPort;
            _logger = logger;
        }

        public async Task Insert(Guild guild)
        {
            var hashEntries = new List<HashEntry>
            {
                new HashEntry(_guildIdField, guild.Id),
                new HashEntry(_nameField, guild.Name)
            };
            await _db.HashSetAsync(FromGuildIdToGuildKey(guild.Id), hashEntries.ToArray());
        }

        public async Task<Guild> Delete(string guildId)
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildKey("*"));

            foreach (var key in allTeamKeys)
            {
                var fetchedTeamData = await _db.HashGetAsync(key, new[] { _guildIdField, _nameField });
                if (fetchedTeamData[0] == guildId)
                {
                    await _db.KeyDeleteAsync(key);
                    return new Guild(fetchedTeamData[0].ToString(), fetchedTeamData[1].ToString());
                }
            }

            return null;
        }

        public async Task<IEnumerable<Guild>> GetAllGuilds()
        {
            var allKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildKey("*"));
            var guilds = new List<Guild>();
            foreach (var key in allKeys)
            {
                var teamId = FromKeyToGuildId(key);
                var fetchedTeamData = await _db.HashGetAsync(key, new[] { _nameField });
                guilds.Add(new Guild(teamId, fetchedTeamData[0]));
            }

            return guilds;
        }

        public async Task<IEnumerable<GuildFplSubscription>> GetAllGuildSubscriptions()
        {
            var allKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildSubKey("*"));
            var guilds = new List<GuildFplSubscription>();
            foreach (var key in allKeys)
            {
                var guildId = FromKeyToGuildId(key);
                var fetchedTeamData = await _db.HashGetAsync(key, new[] { _guildIdField, _channelIdField, _leagueIdField, _subscriptionsField });
                var subs = ParseSubscriptionString(fetchedTeamData[3], " ");
                guilds.Add(new GuildFplSubscription(guildId, fetchedTeamData[1], (int?)fetchedTeamData[2], subs));
            }

            return guilds;
        }

        public async Task<GuildFplSubscription> GetGuildSubscription(string guildId, string channelId)
        {
            var all = await GetAllGuildSubscriptions();
            return all.FirstOrDefault(s => s.GuildId == guildId && s.ChannelId == channelId);
        }

        public async Task DeleteGuildSubscription(string guildId, string channelId)
        {
            var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildSubKey("*"));

            foreach (var key in allTeamKeys)
            {
                var fetchedTeamData = await _db.HashGetAsync(key, new RedisValue[] {_guildIdField, _channelIdField});
                if (fetchedTeamData[0] == guildId && fetchedTeamData[1] == channelId)
                {
                    await _db.KeyDeleteAsync(key);
                }
            }
        }

        public Task UpdateGuildSubscription(GuildFplSubscription guildSub)
        {
            return InsertGuildSubscription(guildSub);
        }

        public async Task InsertGuildSubscription(GuildFplSubscription guildSub)
        {
            var hashEntries = new List<HashEntry>
            {
                new HashEntry(_guildIdField, guildSub.GuildId),
                new HashEntry(_channelIdField, guildSub.ChannelId),
                new HashEntry(_subscriptionsField, string.Join(" ", guildSub.Subscriptions))
            };
            await _db.HashSetAsync(FromGuildIdToGuildSubKey(guildSub.GuildId), hashEntries.ToArray());
        }

        public async Task<IEnumerable<GuildFplSubscription>> GetAllSubscriptionInGuild(string guildId)
        {
            var keys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildSubKey("*"));
            var subs = new List<GuildFplSubscription>();
            foreach (var key in keys)
            {
                var fetchedTeamData = await _db.HashGetAsync(key, new[] { _guildIdField, _channelIdField, _leagueIdField, _subscriptionsField });
                if (fetchedTeamData[0].HasValue)
                {
                    var guildFplSubscription = new GuildFplSubscription(
                        guildId,
                        fetchedTeamData[1],
                        (int?) fetchedTeamData[2],
                        ParseSubscriptionString(fetchedTeamData[2], " "));
                    subs.Add(guildFplSubscription);
                }
            }

            return subs;
        }

        private static string FromGuildIdToGuildKey(string guildId)
        {
            return $"Guild-{guildId}";
        }

        private static string FromGuildIdToGuildSubKey(string guildId)
        {
            return $"GuildSubs-{guildId}";
        }

        private static string FromKeyToGuildId(string key)
        {
            return key.Split('-')[1];
        }

        private static IEnumerable<EventSubscription> ParseSubscriptionString(string subscriptionString, string delimiter)
        {
            var events = new List<EventSubscription>();
            var erroneous = new List<string>();

            if (string.IsNullOrWhiteSpace(subscriptionString))
            {
                return Enumerable.Empty<EventSubscription>();
            }

            var split = subscriptionString.Split(delimiter);
            foreach (var s in split)
            {
                var trimmed = s.Trim();
                if (Enum.TryParse(typeof(EventSubscription), trimmed, true, out var result))
                {
                    events.Add((EventSubscription)result);
                }
                else
                {
                    erroneous.Add(trimmed);
                }
            }

            return events;
        }
    }
}
