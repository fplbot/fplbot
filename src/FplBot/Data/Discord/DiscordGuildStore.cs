using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Data.Discord;

public class DiscordGuildRepository : IGuildRepository
{
    private readonly RedisValue _nameField = "name";
    private readonly RedisValue _guildIdField = "guildid";
    private readonly RedisValue _channelIdField = "channelid";
    private readonly RedisValue _leagueIdField = "leagueid";
    private readonly RedisValue _subscriptionsField = "subs";

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _server;
    private readonly ILogger<DiscordGuildRepository> _logger;

    public DiscordGuildRepository(IConnectionMultiplexer redis, IOptions<DiscordRedisOptions> redisOptions, ILogger<DiscordGuildRepository> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _server = redisOptions.Value.GetRedisServerHostAndPort;
        _logger = logger;
    }

    public async Task<IEnumerable<GuildRepoGuild>> GetAllGuilds()
    {
        var allKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildKey("*"));
        var guilds = new List<GuildRepoGuild>();
        foreach (var key in allKeys)
        {
            var teamId = FromKeyToGuildId(key);
            var fetchedTeamData = await _db.HashGetAsync(key, new[] { _nameField });
            guilds.Add(new GuildRepoGuild(teamId, fetchedTeamData[0]));
        }

        return guilds;
    }

    public async Task<IEnumerable<GuildFplSubscription>> GetAllGuildSubscriptions()
    {
        var allKeys = _redis.GetServer(_server).Keys(pattern: "GuildSubs-*-Channel-*");
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
        var allKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId));
        var guilds = new List<GuildFplSubscription>();
        foreach (var key in allKeys)
        {
            var fetchedTeamData = await _db.HashGetAsync(key, new[] { _guildIdField, _channelIdField, _leagueIdField, _subscriptionsField });
            var subs = ParseSubscriptionString(fetchedTeamData[3], " ");
            guilds.Add(new GuildFplSubscription(guildId, fetchedTeamData[1], (int?)fetchedTeamData[2], subs));
        }

        return guilds.FirstOrDefault();
    }

    public async Task DeleteGuildSubscription(string guildId, string channelId)
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern:FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId));

        foreach (var key in allTeamKeys)
        {
            await _db.KeyDeleteAsync(key);
        }
    }

    public async Task UpdateGuildSubscription(GuildFplSubscription guildSub)
    {
        await InsertGuildSubscription(guildSub);
    }

    public async Task InsertGuildSubscription(GuildFplSubscription guildSub)
    {
        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_guildIdField, guildSub.GuildId),
            new HashEntry(_channelIdField, guildSub.ChannelId),
            new HashEntry(_subscriptionsField, string.Join(" ", guildSub.Subscriptions))
        };

        if (guildSub.LeagueId != null)
            hashEntries.Add(new HashEntry(_leagueIdField, guildSub.LeagueId));

        await _db.HashSetAsync(FromGuildIdAndChannelToGuildChannelSubKey(guildSub.GuildId, guildSub.ChannelId), hashEntries.ToArray());
    }

    public async Task DeleteGuild(string guildId)
    {
        await _db.KeyDeleteAsync(FromGuildIdToGuildKey(guildId));
    }

    private static string FromGuildIdToGuildKey(string guildId)
    {
        return $"Guild-{guildId}";
    }

    private static string FromGuildIdAndChannelToGuildChannelSubKey(string guildId, string channelId)
    {
        return $"GuildSubs-{guildId}-Channel-{channelId}";
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
