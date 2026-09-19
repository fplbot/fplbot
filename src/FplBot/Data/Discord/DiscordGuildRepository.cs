using System.Collections.Concurrent;
using FplBot.Domain;
using StackExchange.Redis;

namespace FplBot.Data.Discord;

public class DiscordGuildRepository(IConnectionMultiplexer redis, ILogger<DiscordGuildRepository> logger) : IGuildRepository
{
    private const string GuildIndexKey = "GuildIndex";
    private const string PlatformPrefix = "discord";
    private const int MaxConcurrentGuildFetches = 64;

    private readonly RedisValue _nameField = "name";
    private readonly RedisValue _idField = "id";
    private readonly RedisValue _guildIdField = "guildid";
    private readonly RedisValue _channelIdField = "channelid";
    private readonly RedisValue _leagueIdField = "leagueid";
    private readonly RedisValue _subscriptionsField = "subs";
    private readonly RedisValue _failureCountField = "failureCount";
    private readonly RedisValue _failingSinceField = "failingSince";
    private readonly RedisValue _lastFailureReasonField = "lastFailureReason";

    private readonly IDatabase _db = redis.GetDatabase();
    private readonly ILogger<DiscordGuildRepository> _logger = logger;

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

        var fetched = await _db.HashGetAsync(key, [_nameField, _idField]);
        var channels = await GetChannelSubscriptions(teamId);
        return Installation.Load(ToInstallationId(fetched[1]), teamId, fetched[0].ToString() ?? string.Empty, token: null, channels);
    }

    public async Task<IEnumerable<Installation>> GetAllInstallations()
    {
        var guildIds = await _db.SetMembersAsync(GuildIndexKey);
        var installations = new ConcurrentBag<Installation>();

        await Parallel.ForEachAsync(guildIds, new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentGuildFetches }, async (guildIdValue, _) =>
        {
            var guildId = guildIdValue.ToString();
            var fetched = await _db.HashGetAsync(FromGuildIdToGuildKey(guildId), [_nameField, _idField]);
            var channels = await GetChannelSubscriptions(guildId);
            installations.Add(Installation.Load(ToInstallationId(fetched[1]), guildId, fetched[0].ToString() ?? string.Empty, token: null, channels));
        });

        return installations;
    }

    public async Task Save(Installation installation)
    {
        var storedChannelIds = (await _db.SetMembersAsync(ToChannelSubIndexKey(installation.ExternalId)))
            .Select(v => v.ToString() ?? string.Empty)
            .ToHashSet();

        var hashEntries = new HashEntry[]
        {
            new(_guildIdField, installation.ExternalId), new(_nameField, installation.Name), new(_idField, installation.Id.Value)
        };
        await _db.HashSetAsync(FromGuildIdToGuildKey(installation.ExternalId), hashEntries);
        await _db.SetAddAsync(GuildIndexKey, installation.ExternalId);
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

    public async Task<IEnumerable<(string InstallationId, string ChannelId)>> GetChannelsSubscribedTo(params FplEvent[] fplEvents)
    {
        var keys = fplEvents.Select(e => (RedisKey)ToEventIndexKey(e)).ToArray();
        var entries = await _db.SetCombineAsync(SetOperation.Union, keys);
        return entries.Select(ParseEventIndexEntry);
    }

    public async Task Delete(Installation installation)
    {
        var channels = await GetChannelSubscriptions(installation.ExternalId);
        foreach (var channel in channels)
        {
            await DeleteChannelSubscription(installation.ExternalId, channel.ChannelId);
        }

        await _db.KeyDeleteAsync(ToChannelSubIndexKey(installation.ExternalId));
        await _db.SetRemoveAsync(GuildIndexKey, installation.ExternalId);
        await _db.KeyDeleteAsync(ToInstallationIdIndexKey(installation.Id.Value));
        await _db.KeyDeleteAsync(FromGuildIdToGuildKey(installation.ExternalId));
    }

    public async Task SaveChannelSubscription(string guildId, ChannelSubscription channel)
    {
        var key = FromGuildIdAndChannelToGuildChannelSubKey(guildId, channel.ChannelId);
        var oldEvents = ExpandEvents(ParseSubscriptionString((await _db.HashGetAsync(key, _subscriptionsField)).ToString(), " ").Select(ToDomainEvent));
        var newEvents = ExpandEvents(channel.Events.Current);

        var hashEntries = new List<HashEntry>
        {
            new(_guildIdField, guildId),
            new(_idField, channel.Id.Value),
            new(_channelIdField, channel.ChannelId),
            new(_subscriptionsField, string.Join(" ", channel.Events.Current.Select(ToStorageEvent))),
            new(_failureCountField, channel.FailureCount)
        };

        if (channel.FollowedLeagueId is { } leagueId)
        {
            hashEntries.Add(new HashEntry(_leagueIdField, (int)leagueId.Value));
        }

        if (channel.FailingSince is { } failingSince)
        {
            hashEntries.Add(new HashEntry(_failingSinceField, failingSince.ToUnixTimeMilliseconds()));
            hashEntries.Add(new HashEntry(_lastFailureReasonField, channel.LastFailureReason));
        }

        // One transaction, so a concurrent reader sees the subscription as it was or as it now is,
        // never a failure counted with no date on it yet.
        var transaction = _db.CreateTransaction();
        _ = transaction.HashSetAsync(key, [.. hashEntries]);
        if (channel.FollowedLeagueId is null)
        {
            _ = transaction.HashDeleteAsync(key, _leagueIdField);
        }

        if (channel.FailingSince is null)
        {
            _ = transaction.HashDeleteAsync(key, [_failingSinceField, _lastFailureReasonField]);
        }

        await transaction.ExecuteAsync();

        await _db.SetAddAsync(ToChannelSubIndexKey(guildId), channel.ChannelId);
        await _db.StringSetAsync(ToSubIdIndexKey(channel.Id.Value), ToSubIndexEntry(guildId, channel.ChannelId));
        await UpdateEventIndex(guildId, channel.ChannelId, oldEvents, newEvents);
    }

    public async Task DeleteChannelSubscription(string guildId, string channelId)
    {
        var key = FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId);
        var events = ExpandEvents(ParseSubscriptionString((await _db.HashGetAsync(key, _subscriptionsField)).ToString(), " ").Select(ToDomainEvent));
        await UpdateEventIndex(guildId, channelId, events, []);

        await RemoveSubIdIndexPointingAt(await _db.HashGetAsync(key, _idField), guildId, channelId);

        await _db.KeyDeleteAsync(key);
        await _db.SetRemoveAsync(ToChannelSubIndexKey(guildId), channelId);
    }

    // Save() diff-syncs by channel id, so a moved subscription looks like "old channel gone, new
    // channel added". The subscription keeps its id across a move, so deleting the old channel must
    // not take the reverse index entry with it - by then it already points at the new channel.
    private async Task RemoveSubIdIndexPointingAt(RedisValue subId, string guildId, string channelId)
    {
        if (!subId.HasValue)
        {
            return;
        }

        var indexKey = ToSubIdIndexKey(subId!);
        if (await _db.StringGetAsync(indexKey) == ToSubIndexEntry(guildId, channelId))
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

    private static string ToInstallationIndexEntry(string guildId) => $"{PlatformPrefix}:{guildId}";

    private static string ToSubIdIndexKey(string subId) => $"SubId-{subId}";

    private static string ToSubIndexEntry(string guildId, string channelId) => $"{PlatformPrefix}:{guildId}:{channelId}";

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

    private async Task UpdateEventIndex(string guildId, string channelId, IEnumerable<FplEvent> oldEvents, IEnumerable<FplEvent> newEvents)
    {
        var entry = ToEventIndexEntry(guildId, channelId);
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

    private static string ToEventIndexKey(FplEvent fplEvent) => $"GuildEventIndex-{fplEvent}";

    private static string ToEventIndexEntry(string guildId, string channelId) => $"{guildId}:{channelId}";

    private static (string InstallationId, string ChannelId) ParseEventIndexEntry(RedisValue value)
    {
        var parts = value.ToString().Split(':', 2);
        return (parts[0], parts[1]);
    }

    public Task<ChannelSubscription?> GetChannelSubscription(string installationId, string channelId) =>
        ReadChannelSubscription(installationId, channelId);

    public async Task<IEnumerable<(string InstallationId, string ChannelId, ClassicLeagueId LeagueId)>> GetChannelsFollowingALeague()
    {
        var guildIds = (await _db.SetMembersAsync(GuildIndexKey)).Select(g => g.ToString()).ToList();

        var channelBatch = _db.CreateBatch();
        var channelReads = guildIds.ToDictionary(g => g, g => channelBatch.SetMembersAsync(ToChannelSubIndexKey(g)));
        channelBatch.Execute();
        await Task.WhenAll(channelReads.Values);

        var leagueBatch = _db.CreateBatch();
        var leagueReads = new List<(string GuildId, string ChannelId, Task<RedisValue> Read)>();
        foreach (var (guildId, channelRead) in channelReads)
        {
            foreach (var channelId in channelRead.Result.Select(c => c.ToString()))
            {
                leagueReads.Add((guildId, channelId,
                    leagueBatch.HashGetAsync(FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId), _leagueIdField)));
            }
        }

        leagueBatch.Execute();
        await Task.WhenAll(leagueReads.Select(r => r.Read));

        return leagueReads
            .Where(r => r.Read.Result.TryParse(out long _))
            .Select(r => (r.GuildId, r.ChannelId, new ClassicLeagueId((long)r.Read.Result)))
            .ToList();
    }

    private async Task<IEnumerable<ChannelSubscription>> GetChannelSubscriptions(string guildId)
    {
        var channelIds = await _db.SetMembersAsync(ToChannelSubIndexKey(guildId));
        var result = new List<ChannelSubscription>();
        foreach (var channelIdValue in channelIds)
        {
            var sub = await ReadChannelSubscription(guildId, channelIdValue.ToString()!);
            if (sub is not null)
            {
                result.Add(sub);
            }
        }

        return result;
    }

    private async Task<ChannelSubscription?> ReadChannelSubscription(string guildId, string channelId)
    {
        var fetched = await _db.HashGetAsync(FromGuildIdAndChannelToGuildChannelSubKey(guildId, channelId),
        [
            _channelIdField, _leagueIdField, _subscriptionsField, _failureCountField, _failingSinceField,
            _lastFailureReasonField, _idField
        ]);
        if (!fetched[0].HasValue)
        {
            return null;
        }

        var leagueId = fetched[1].HasValue ? (int?)fetched[1] : null;
        var subs = ParseSubscriptionString(fetched[2].ToString(), " ");
        var domainLeagueId = leagueId is { } id ? new ClassicLeagueId(id) : null;
        var failureCount = fetched[3].HasValue ? (int)fetched[3] : 0;
        var failingSince = fetched[4].HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds((long)fetched[4])
            : (DateTimeOffset?)null;
        var lastFailureReason = fetched[5].HasValue ? fetched[5].ToString() : null;
        return ChannelSubscription.Load(ToSubscriptionId(fetched[6]), channelId, domainLeagueId, subs.Select(ToDomainEvent), failureCount, failingSince,
            lastFailureReason);
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
