using FplBot.Domain;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Data.Discord;

public class DiscordGuildRepository : IGuildRepository
{
    private const string GuildIndexKey = "GuildIndex";

    private readonly RedisValue _nameField = "name";
    private readonly RedisValue _guildIdField = "guildid";
    private readonly RedisValue _channelIdField = "channelid";
    private readonly RedisValue _leagueIdField = "leagueid";
    private readonly RedisValue _subscriptionsField = "subs";

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _server;
    private readonly ILogger<DiscordGuildRepository> _logger;

    public DiscordGuildRepository(IConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions, ILogger<DiscordGuildRepository> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _server = redisOptions.Value.GetRedisServerHostAndPort;
        _logger = logger;
    }

    public async Task<Installation> GetInstallation(string teamId)
    {
        var installation = await FindInstallationByTeamId(teamId);
        if (installation is null)
        {
            throw new KeyNotFoundException($"No Discord guild found for id '{teamId}'");
        }
        return installation;
    }

    public async Task<Installation?> FindInstallationByTeamId(string teamId)
    {
        var key = FromGuildIdToGuildKey(teamId);
        if (!await _db.KeyExistsAsync(key))
        {
            return null;
        }

        var fetched = await _db.HashGetAsync(key, [_nameField]);
        var channels = await GetChannelSubscriptions(teamId);
        return Installation.Load(teamId, fetched[0].ToString() ?? string.Empty, token: null, channels);
    }

    public async Task<IEnumerable<Installation>> GetAllInstallations()
    {
        var allKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdToGuildKey("*"));
        var installations = new List<Installation>();
        foreach (var key in allKeys)
        {
            var guildId = FromKeyToGuildId(key);
            var fetched = await _db.HashGetAsync(key, [_nameField]);
            var channels = await GetChannelSubscriptions(guildId);
            installations.Add(Installation.Load(guildId, fetched[0].ToString() ?? string.Empty, token: null, channels));
        }

        return installations;
    }

    public async Task Save(Installation installation)
    {
        var storedChannelIds = (await GetChannelSubscriptions(installation.Id))
            .Select(c => c.ChannelId)
            .ToHashSet();

        var hashEntries = new HashEntry[] { new(_guildIdField, installation.Id), new(_nameField, installation.Name) };
        await _db.HashSetAsync(FromGuildIdToGuildKey(installation.Id), hashEntries);
        await _db.SetAddAsync(GuildIndexKey, installation.Id);

        var currentChannelIds = installation.ChannelSubscriptions.Select(c => c.ChannelId).ToHashSet();

        foreach (var channel in installation.ChannelSubscriptions)
        {
            await SaveChannelSubscription(installation.Id, channel);
        }

        foreach (var removedChannelId in storedChannelIds.Except(currentChannelIds))
        {
            await DeleteChannelSubscription(installation.Id, removedChannelId);
        }
    }

    public async Task Delete(Installation installation)
    {
        var channels = await GetChannelSubscriptions(installation.Id);
        foreach (var channel in channels)
        {
            await DeleteChannelSubscription(installation.Id, channel.ChannelId);
        }
        await _db.KeyDeleteAsync(ToChannelSubIndexKey(installation.Id));
        await _db.SetRemoveAsync(GuildIndexKey, installation.Id);
        await _db.KeyDeleteAsync(FromGuildIdToGuildKey(installation.Id));
    }

    private async Task SaveChannelSubscription(string guildId, ChannelSubscription channel)
    {
        var hashEntries = new List<HashEntry>
        {
            new(_guildIdField, guildId),
            new(_channelIdField, channel.ChannelId),
            new(_subscriptionsField, string.Join(" ", channel.Events.Current.Select(ToStorageEvent)))
        };

        if (channel.FollowedLeagueId is { } leagueId)
        {
            hashEntries.Add(new HashEntry(_leagueIdField, (int)leagueId.Value));
        }

        await _db.HashSetAsync(FromGuildIdAndChannelToGuildChannelSubKey(guildId, channel.ChannelId), hashEntries.ToArray());
        await _db.SetAddAsync(ToChannelSubIndexKey(guildId), channel.ChannelId);
    }

    private async Task DeleteChannelSubscription(string guildId, string channelId)
    {
        await _db.KeyDeleteAsync(FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId));
        await _db.SetRemoveAsync(ToChannelSubIndexKey(guildId), channelId);
    }

    private async Task<IEnumerable<ChannelSubscription>> GetChannelSubscriptions(string guildId)
    {
        var allKeys = _redis.GetServer(_server).Keys(pattern: FromGuildIdAndChannelToGuildChannelSubKey(guildId, "*"));
        var result = new List<ChannelSubscription>();
        foreach (var key in allKeys)
        {
            var fetched = await _db.HashGetAsync(key, [_channelIdField, _leagueIdField, _subscriptionsField]);
            var channelId = fetched[0].ToString() ?? string.Empty;
            var leagueId = fetched[1].HasValue ? (int?)fetched[1] : null;
            var subs = ParseSubscriptionString(fetched[2].ToString(), " ");
            var domainLeagueId = leagueId is { } id ? new ClassicLeagueId(id) : null;
            result.Add(ChannelSubscription.Load(channelId, domainLeagueId, subs.Select(ToDomainEvent)));
        }

        return result;
    }

    private static string FromGuildIdToGuildKey(string guildId)
    {
        return $"Guild-{guildId}";
    }

    private static string FromGuildIdAndChannelToGuildChannelSubKey(string guildId, string channelId)
    {
        return $"GuildSubs-{guildId}-Channel-{channelId}";
    }

    private static string ToChannelSubIndexKey(string guildId)
    {
        return $"GuildChannelSubIndex-{guildId}";
    }

    private static string FromKeyToGuildId(string? key)
    {
        return key?.Split('-')[1] ?? string.Empty;
    }

    private static IEnumerable<EventSubscription> ParseSubscriptionString(string? subscriptionString, string delimiter)
    {
        var events = new List<EventSubscription>();
        var erroneous = new List<string>();

        if (string.IsNullOrWhiteSpace(subscriptionString))
        {
            return [];
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

    private static FplEvent ToDomainEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
    private static EventSubscription ToStorageEvent(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
