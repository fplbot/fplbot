using FplBot.Domain;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Data.Slack;

public class SlackTeamRepository : ISlackTeamRepository
{
    private const string TeamIndexKey = "TeamIndex";
    private const string PlatformPrefix = "slack";

    private readonly ILogger<SlackTeamRepository> _logger;

    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _server;

    private readonly string _accessTokenField = "accessToken";
    private readonly string _teamNameField = "teamName";
    private readonly string _teamIdField = "teamId";
    private readonly string _pendingRemovalField = "pendingRemoval";
    private readonly string _idField = "id";

    // Channel subscriptions: one hash per channel, keyed as SlackChannelSub-{teamId}-{channelId}.
    private readonly string _channelSubChannelIdField = "channelId";
    private readonly string _channelSubLeagueIdField = "leagueId";
    private readonly string _channelSubSubscriptionsField = "subscriptions";
    private readonly string _channelSubFailureCountField = "failureCount";
    private readonly string _channelSubFailingSinceField = "failingSince";
    private readonly string _channelSubLastFailureReasonField = "lastFailureReason";

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
        var fetched = await _db.HashGetAsync(FromTeamIdToTeamKey(teamId), [_accessTokenField, _teamNameField, _pendingRemovalField, _idField]);
        var pendingRemoval = fetched[2].HasValue && (bool)fetched[2];
        var channels = await GetChannelSubscriptions(teamId);
        return Installation.Load(ToInstallationId(fetched[3]), teamId, fetched[1]!, fetched[0].ToString() ?? string.Empty, channels, pendingRemoval);
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

        return [.. subs];
    }

    public async Task Save(Installation installation)
    {
        var storedChannelIds = (await _db.SetMembersAsync(ToChannelSubIndexKey(installation.ExternalId)))
            .Select(v => v.ToString() ?? string.Empty)
            .ToHashSet();

        var hashEntries = new HashEntry[]
        {
            new(_accessTokenField, installation.Token),
            new(_teamNameField, installation.Name),
            new(_teamIdField, installation.ExternalId),
            new(_idField, installation.Id.Value),
            new(_pendingRemovalField, installation.PendingRemoval)
        };

        await _db.HashSetAsync(FromTeamIdToTeamKey(installation.ExternalId), hashEntries);
        await _db.SetAddAsync(TeamIndexKey, installation.ExternalId);
        await _db.StringSetAsync(ToInstallationIdIndexKey(installation.Id.Value), ToInstallationIndexEntry(installation.ExternalId));

        var currentChannelIds = installation.ChannelSubscriptions.Select(c => c.ChannelId).ToHashSet();

        foreach (var channel in installation.ChannelSubscriptions)
        {
            await SaveChannelSubscription(installation.ExternalId, channel);
        }

        foreach (var removedChannelId in storedChannelIds.Except(currentChannelIds))
        {
            await DeleteChannelSubscription(installation.ExternalId, removedChannelId);
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
        // ExpandEvents only expands "All" into the concrete FplEvent values that existed at save
        // time, so a channel that saved "All" before a new event was added would be missing from
        // that event's index. Unioning in the "All" index here as well keeps this correct for
        // events added after the fact, without requiring every "All" channel to re-save.
        var keys = fplEvents.Append(FplEvent.All).Distinct().Select(e => (RedisKey)ToEventIndexKey(e)).ToArray();
        var entries = await _db.SetCombineAsync(SetOperation.Union, keys);
        return entries.Select(ParseEventIndexEntry);
    }

    public async Task Delete(Installation installation)
    {
        var teamId = installation.ExternalId;

        var channelIds = await _db.SetMembersAsync(ToChannelSubIndexKey(teamId));
        foreach (var channelId in channelIds)
        {
            await DeleteChannelSubscription(teamId, channelId.ToString()!);
        }

        await _db.KeyDeleteAsync(ToChannelSubIndexKey(teamId));
        await _db.SetRemoveAsync(TeamIndexKey, teamId);
        await _db.KeyDeleteAsync(ToInstallationIdIndexKey(installation.Id.Value));

        await _db.KeyDeleteAsync(FromTeamIdToTeamKey(teamId));
    }

    public async Task<IEnumerable<string>> GetTokens()
    {
        var allTeamKeys = _redis.GetServer(_server).Keys(pattern: FromTeamIdToTeamKey("*"));
        var tokens = new List<string>();
        foreach (var key in allTeamKeys)
        {
            var token = await _db.HashGetAsync(key, _accessTokenField);
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
            new HashEntry(_idField, channel.Id.Value),
            new HashEntry(_channelSubChannelIdField, channel.ChannelId),
            new HashEntry(_channelSubSubscriptionsField, string.Join(" ", subscriptions)),
            new HashEntry(_channelSubFailureCountField, channel.FailureCount)
        };

        if (channel.FollowedLeagueId is { } leagueId)
        {
            hashEntries.Add(new HashEntry(_channelSubLeagueIdField, (int)leagueId.Value));
        }

        if (channel.FailingSince is { } failingSince)
        {
            hashEntries.Add(new HashEntry(_channelSubFailingSinceField, failingSince.ToUnixTimeMilliseconds()));
            hashEntries.Add(new HashEntry(_channelSubLastFailureReasonField, channel.LastFailureReason));
        }

        // One transaction, so a concurrent reader sees the subscription as it was or as it now is,
        // never a failure counted with no date on it yet.
        var transaction = _db.CreateTransaction();
        _ = transaction.HashSetAsync(key, [.. hashEntries]);
        if (channel.FollowedLeagueId is null)
        {
            _ = transaction.HashDeleteAsync(key, _channelSubLeagueIdField);
        }

        if (channel.FailingSince is null)
        {
            _ = transaction.HashDeleteAsync(key,
                [(RedisValue)_channelSubFailingSinceField, (RedisValue)_channelSubLastFailureReasonField]);
        }

        await transaction.ExecuteAsync();

        await _db.SetAddAsync(ToChannelSubIndexKey(teamId), channel.ChannelId);
        await _db.StringSetAsync(ToSubIdIndexKey(channel.Id.Value), ToSubIndexEntry(teamId, channel.ChannelId));
        await UpdateEventIndex(teamId, channel.ChannelId, oldEvents, newEvents);
    }

    // A channel subscribed via FplEvent.All (EventCollection's short-circuit sentinel, see
    // EventCollection.Contains) is stored as the single literal "All" in the subscriptions hash
    // field, not as every concrete event name. Per-event index membership has to be based on the
    // effective (expanded) set, or an "All"-subscribed channel would silently vanish from every
    // concrete event's index.
    private static IEnumerable<FplEvent> ExpandEvents(IEnumerable<FplEvent> events)
    {
        var materialized = events as ICollection<FplEvent> ?? [.. events];
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

    public async Task<IEnumerable<(string InstallationId, string ChannelId, ClassicLeagueId LeagueId)>> GetChannelsFollowingALeague()
    {
        var teamIds = (await _db.SetMembersAsync(TeamIndexKey)).Select(t => t.ToString()).ToList();

        var channelBatch = _db.CreateBatch();
        var channelReads = teamIds.ToDictionary(t => t, t => channelBatch.SetMembersAsync(ToChannelSubIndexKey(t)));
        channelBatch.Execute();
        await Task.WhenAll(channelReads.Values);

        var leagueBatch = _db.CreateBatch();
        var leagueReads = new List<(string TeamId, string ChannelId, Task<RedisValue> Read)>();
        foreach (var (teamId, channelRead) in channelReads)
        {
            foreach (var channelId in channelRead.Result.Select(c => c.ToString()))
            {
                leagueReads.Add((teamId, channelId,
                    leagueBatch.HashGetAsync(FromTeamAndChannelToChannelSubKey(teamId, channelId), _channelSubLeagueIdField)));
            }
        }

        leagueBatch.Execute();
        await Task.WhenAll(leagueReads.Select(r => r.Read));

        return leagueReads
            .Where(r => r.Read.Result.TryParse(out long _))
            .Select(r => (r.TeamId, r.ChannelId, new ClassicLeagueId((long)r.Read.Result)))
            .ToList();
    }

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
        [
            _channelSubChannelIdField, _channelSubLeagueIdField, _channelSubSubscriptionsField,
            _channelSubFailureCountField, _channelSubFailingSinceField, _channelSubLastFailureReasonField,
            _idField
        ]);
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
        var lastFailureReason = fetched[5].HasValue ? fetched[5].ToString() : null;
        return ChannelSubscription.Load(ToSubscriptionId(fetched[6]), channelId, domainLeagueId, subs.Select(ToDomainEvent), failureCount, failingSince,
            lastFailureReason);
    }

    public async Task DeleteChannelSubscription(string teamId, string channelId)
    {
        var key = FromTeamAndChannelToChannelSubKey(teamId, channelId);
        var events = ExpandEvents(GetSubscriptions(teamId, await _db.HashGetAsync(key, _channelSubSubscriptionsField)).Select(ToDomainEvent));
        await UpdateEventIndex(teamId, channelId, events, []);

        await RemoveSubIdIndexPointingAt(await _db.HashGetAsync(key, _idField), teamId, channelId);

        await _db.KeyDeleteAsync(key);
        await _db.SetRemoveAsync(ToChannelSubIndexKey(teamId), channelId);
    }

    // Save() diff-syncs by channel id, so a moved subscription looks like "old channel gone, new
    // channel added". The subscription keeps its id across a move, so deleting the old channel must
    // not take the reverse index entry with it - by then it already points at the new channel.
    private async Task RemoveSubIdIndexPointingAt(RedisValue subId, string teamId, string channelId)
    {
        if (!subId.HasValue)
        {
            return;
        }

        var indexKey = ToSubIdIndexKey(subId!);
        if (await _db.StringGetAsync(indexKey) == ToSubIndexEntry(teamId, channelId))
        {
            await _db.KeyDeleteAsync(indexKey);
        }
    }

    // Rows written before internal ids existed have no id field; backfill-internal-ids gives them one.
    // Until it runs they get an id that lives only for this instance, deliberately without writing it
    // back: a read must never write, or a hash deleted mid-read is resurrected by the very lookup
    // checking whether it is gone.
    private static InstallationId ToInstallationId(RedisValue stored) =>
        stored.HasValue ? new InstallationId(stored!) : InstallationId.New();

    private static SubscriptionId ToSubscriptionId(RedisValue stored) =>
        stored.HasValue ? new SubscriptionId(stored!) : SubscriptionId.New();

    private static string ToInstallationIdIndexKey(string id) => $"InstallationId-{id}";

    private static string ToInstallationIndexEntry(string teamId) => $"{PlatformPrefix}:{teamId}";

    private static string ToSubIdIndexKey(string subId) => $"SubId-{subId}";

    private static string ToSubIndexEntry(string teamId, string channelId) => $"{PlatformPrefix}:{teamId}:{channelId}";

    private static string FromTeamAndChannelToChannelSubKey(string teamId, string channelId) =>
        $"SlackChannelSub-{teamId}-{channelId}";

    private static string ToChannelSubIndexKey(string teamId) =>
        $"SlackChannelSubIndex-{teamId}";

    private static EventSubscription ToStorageEvent(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
