using FplBot.Domain;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Data.Slack;

public class SlackTeamRepository : ISlackTeamRepository
{
    private const string TeamIndexKey = "TeamIndex";

    private readonly ILogger<SlackTeamRepository> _logger;

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _server;

    private readonly string _accessTokenField = "accessToken";
    private readonly string _teamNameField = "teamName";
    private readonly string _teamIdField = "teamId";
    private readonly string _pendingRemovalField = "pendingRemoval";

    // Channel subscriptions: one hash per channel, keyed as SlackChannelSub-{teamId}-{channelId}.
    private readonly string _channelSubChannelIdField = "channelId";
    private readonly string _channelSubLeagueIdField = "leagueId";
    private readonly string _channelSubSubscriptionsField = "subscriptions";
    private readonly string _channelSubFailureCountField = "failureCount";
    private readonly string _channelSubFailingSinceField = "failingSince";

    public SlackTeamRepository(IConnectionMultiplexer redis, IOptions<RedisOptions> redisOptions, ILogger<SlackTeamRepository> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _server = redisOptions.Value.GetRedisServerHostAndPort;
        _logger = logger;
    }

    public async Task<Installation> GetInstallation(string teamId)
    {
        if (!await _db.KeyExistsAsync(FromTeamIdToTeamKey(teamId)))
        {
            throw new KeyNotFoundException($"No Slack installation found for team id '{teamId}'");
        }

        return await LoadInstallation(teamId);
    }

    private async Task<Installation> LoadInstallation(string teamId)
    {
        var fetched = await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), [_accessTokenField, _teamNameField, _pendingRemovalField]);
        var pendingRemoval = fetched[2].HasValue && (bool)fetched[2];
        var channels = await GetChannelSubscriptions(teamId);
        return Installation.Load(teamId, fetched[1]!, fetched[0].ToString() ?? string.Empty, channels, pendingRemoval);
    }

    private static FplEvent ToDomainEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());

    private List<EventSubscription> GetSubscriptions(string teamId, RedisValue fetchedTeamData)
    {
        if (!fetchedTeamData.HasValue)
        {
            return [];
        }

        (var subs, var unableToParse) = fetchedTeamData.ToString().ParseSubscriptionString(delimiter: " ");

        if (unableToParse.Any())
        {
            _logger.LogError("Unable to parse events for team {team}: {unableToParse}", teamId, string.Join(", ", unableToParse));
        }

        return subs.ToList();
    }

    public async Task Save(Installation installation)
    {
        var storedChannelIds = (await _db.SetMembersAsync(ToChannelSubIndexKey(installation.Id)))
            .Select(v => v.ToString() ?? string.Empty)
            .ToHashSet();

        var hashEntries = new HashEntry[]
        {
            new(_accessTokenField, installation.Token),
            new(_teamNameField, installation.Name),
            new(_teamIdField, installation.Id),
            new(_pendingRemovalField, installation.PendingRemoval)
        };

        await _db.HashSetAsync(FromTeamIdToTeamKey(installation.Id), hashEntries);
        await _db.SetAddAsync(TeamIndexKey, installation.Id);

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

    public async Task<Installation?> FindInstallationByTeamId(string teamId)
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));

        foreach (var key in allTeamKeys)
        {
            var storedTeamId = FromKeyToTeamId(key.ToString());
            if (string.Compare(storedTeamId, teamId, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                continue;
            }

            return await LoadInstallation(storedTeamId);
        }

        return null;
    }

    public async Task<IEnumerable<(string InstallationId, string ChannelId)>> GetChannelsSubscribedTo(params FplEvent[] fplEvents)
    {
        var keys = fplEvents.Select(e => (RedisKey)ToEventIndexKey(e)).ToArray();
        var entries = await _db.SetCombineAsync(SetOperation.Union, keys);
        return entries.Select(ParseEventIndexEntry);
    }

    public async Task Delete(Installation installation)
    {
        var teamId = installation.Id;

        var channelIds = await _db.SetMembersAsync(ToChannelSubIndexKey(teamId));
        foreach (var channelId in channelIds)
        {
            await DeleteChannelSubscription(teamId, channelId.ToString()!);
        }
        await _db.KeyDeleteAsync(ToChannelSubIndexKey(teamId));
        await _db.SetRemoveAsync(TeamIndexKey, teamId);

        await _db.KeyDeleteAsync(FromTeamIdToTeamKey(teamId));
    }

    public async Task<IEnumerable<string>> GetTokens()
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
        var tokens = new List<string>();
        foreach (var key in allTeamKeys)
        {
            var token = await _db.HashGetAsync(key,_accessTokenField);
            tokens.Add(token.ToString() ?? string.Empty);
        }
        return tokens.Select(t => t.ToString());
    }

    public async Task<string?> GetTokenByTeamId(string teamId)
    {
        return await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), _accessTokenField);
    }
    private static string FromTeamIdToTeamKey(string teamId)
    {
        return $"TeamId-{teamId}";
    }

    private static string FromKeyToTeamId(string key)
    {
        return key.Substring(key.IndexOf('-') + 1);
    }

    public async Task<IEnumerable<Installation>> GetAllInstallations()
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
        var installations = new List<Installation>();
        foreach (var key in allTeamKeys)
        {
            var teamId = FromKeyToTeamId(key.ToString());
            installations.Add(await LoadInstallation(teamId));
        }

        return installations;
    }

    public async Task SaveChannelSubscription(string teamId, ChannelSubscription channel)
    {
        var key = FromTeamAndChannelToChannelSubKey(teamId, channel.ChannelId);
        var oldEvents = ExpandEvents(GetSubscriptions(teamId, await _db.HashGetAsync(key, _channelSubSubscriptionsField)).Select(ToDomainEvent));
        var newEvents = ExpandEvents(channel.Events.Current);

        var subscriptions = channel.Events.Current.Select(ToStorageEvent);

        var hashEntries = new List<HashEntry>
        {
            new HashEntry(_teamIdField, teamId),
            new HashEntry(_channelSubChannelIdField, channel.ChannelId),
            new HashEntry(_channelSubSubscriptionsField, string.Join(" ", subscriptions)),
            new HashEntry(_channelSubFailureCountField, channel.FailureCount)
        };

        if (channel.FollowedLeagueId is { } leagueId)
        {
            hashEntries.Add(new HashEntry(_channelSubLeagueIdField, (int)leagueId.Value));
        }

        await _db.HashSetAsync(key, hashEntries.ToArray());
        if (channel.FailingSince is { } failingSince)
        {
            await _db.HashSetAsync(key, _channelSubFailingSinceField, failingSince.ToUnixTimeMilliseconds());
        }
        else
        {
            await _db.HashDeleteAsync(key, _channelSubFailingSinceField);
        }
        await _db.SetAddAsync(ToChannelSubIndexKey(teamId), channel.ChannelId);
        await UpdateEventIndex(teamId, channel.ChannelId, oldEvents, newEvents);
    }

    // A channel subscribed via FplEvent.All (EventCollection's short-circuit sentinel, see
    // EventCollection.Contains) is stored as the single literal "All" in the subscriptions hash
    // field, not as every concrete event name. Per-event index membership has to be based on the
    // effective (expanded) set, or an "All"-subscribed channel would silently vanish from every
    // concrete event's index.
    private static IEnumerable<FplEvent> ExpandEvents(IEnumerable<FplEvent> events)
    {
        var materialized = events as ICollection<FplEvent> ?? events.ToList();
        return materialized.Contains(FplEvent.All)
            ? Enum.GetValues<FplEvent>().Where(e => e != FplEvent.All)
            : materialized;
    }

    private async Task UpdateEventIndex(string teamId, string channelId, IEnumerable<FplEvent> oldEvents, IEnumerable<FplEvent> newEvents)
    {
        var entry = ToEventIndexEntry(teamId, channelId);
        var oldSet = oldEvents.ToHashSet();
        var newSet = newEvents.ToHashSet();

        foreach (var removed in oldSet.Except(newSet))
        {
            await _db.SetRemoveAsync(ToEventIndexKey(removed), entry);
        }

        foreach (var added in newSet.Except(oldSet))
        {
            await _db.SetAddAsync(ToEventIndexKey(added), entry);
        }
    }

    private static string ToEventIndexKey(FplEvent fplEvent) => $"SlackEventIndex-{fplEvent}";

    private static string ToEventIndexEntry(string teamId, string channelId) => $"{teamId}:{channelId}";

    private static (string InstallationId, string ChannelId) ParseEventIndexEntry(RedisValue value)
    {
        var parts = value.ToString().Split(':', 2);
        return (parts[0], parts[1]);
    }

    public Task<ChannelSubscription?> GetChannelSubscription(string installationId, string channelId) =>
        ReadChannelSubscription(installationId, channelId);

    // Reads the exact set of channel ids this team has saved, then fetches each channel's hash by
    // its exact key. Deliberately avoids a KEYS pattern scan on "SlackChannelSub-{teamId}-*": that
    // glob also matches OTHER teams whose id happens to start with this team's id plus a dash
    // (e.g. scanning for "DEV-SLACK" would also match "DEV-SLACK-2", "DEV-SLACK-BARE", ...).
    private async Task<IEnumerable<ChannelSubscription>> GetChannelSubscriptions(string teamId)
    {
        var channelIds = await _db.SetMembersAsync(ToChannelSubIndexKey(teamId));
        var result = new List<ChannelSubscription>();

        foreach (var channelIdValue in channelIds)
        {
            var sub = await ReadChannelSubscription(teamId, channelIdValue.ToString()!);
            if (sub is not null)
            {
                result.Add(sub);
            }
        }

        return result;
    }

    private async Task<ChannelSubscription?> ReadChannelSubscription(string teamId, string channelId)
    {
        var fetched = await _db.HashGetAsync(FromTeamAndChannelToChannelSubKey(teamId, channelId),
            [_channelSubChannelIdField, _channelSubLeagueIdField, _channelSubSubscriptionsField,
             _channelSubFailureCountField, _channelSubFailingSinceField]);
        if (!fetched[0].HasValue)
        {
            return null;
        }

        int? leagueId = fetched[1].HasValue ? int.Parse(fetched[1]!) : null;
        var subs = GetSubscriptions(teamId, fetched[2]);
        var domainLeagueId = leagueId is { } id ? new ClassicLeagueId(id) : null;
        var failureCount = fetched[3].HasValue ? (int)fetched[3] : 0;
        var failingSince = fetched[4].HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds((long)fetched[4])
            : (DateTimeOffset?)null;
        return ChannelSubscription.Load(channelId, domainLeagueId, subs.Select(ToDomainEvent), failureCount, failingSince);
    }

    public async Task DeleteChannelSubscription(string teamId, string channelId)
    {
        var key = FromTeamAndChannelToChannelSubKey(teamId, channelId);
        var events = ExpandEvents(GetSubscriptions(teamId, await _db.HashGetAsync(key, _channelSubSubscriptionsField)).Select(ToDomainEvent));
        await UpdateEventIndex(teamId, channelId, events, []);

        await _db.KeyDeleteAsync(key);
        await _db.SetRemoveAsync(ToChannelSubIndexKey(teamId), channelId);
    }

    private static string FromTeamAndChannelToChannelSubKey(string teamId, string channelId) =>
        $"SlackChannelSub-{teamId}-{channelId}";

    private static string ToChannelSubIndexKey(string teamId) =>
        $"SlackChannelSubIndex-{teamId}";

    private static EventSubscription ToStorageEvent(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
