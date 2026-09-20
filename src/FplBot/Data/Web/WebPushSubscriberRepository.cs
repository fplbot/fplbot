using FplBot.Domain;
using StackExchange.Redis;

namespace FplBot.Data.Web;

public class WebPushSubscriberRepository(IConnectionMultiplexer redis) : IWebPushSubscriberRepository
{
    private const string IndexKey = "WebPushSubIndex";

    private static readonly RedisValue IdField = "id";
    private static readonly RedisValue NameField = "name";
    private static readonly RedisValue EndpointField = "endpoint";
    private static readonly RedisValue P256dhField = "p256dh";
    private static readonly RedisValue AuthField = "auth";
    private static readonly RedisValue LeagueIdField = "leagueid";
    private static readonly RedisValue SubsField = "subs";
    private static readonly RedisValue CreatedField = "created";

    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<WebPushSubscriber?> Find(WebPushSubscriberId id)
    {
        var fetched = await _db.HashGetAsync(SubscriberKey(id),
            [NameField, EndpointField, P256dhField, AuthField, LeagueIdField, SubsField]);

        return fetched[1].HasValue
            ? WebPushSubscriber.Load(
                id,
                new PushKeys(fetched[1]!, fetched[2]!, fetched[3]!),
                fetched[0].HasValue ? fetched[0].ToString() : null,
                fetched[4].HasValue ? new ClassicLeagueId((long)fetched[4]) : null,
                ParseEvents(fetched[5]))
            : null;
    }

    public async Task Save(WebPushSubscriber subscriber)
    {
        var key = SubscriberKey(subscriber.Id);
        var existing = await _db.HashGetAsync(key, SubsField);

        var entries = new List<HashEntry>
        {
            new(IdField, subscriber.Id.Value),
            new(EndpointField, subscriber.PushKeys.Endpoint),
            new(P256dhField, subscriber.PushKeys.P256dh),
            new(AuthField, subscriber.PushKeys.Auth),
            new(SubsField, string.Join(" ", subscriber.Events.Current.Select(e => e.ToString())))
        };

        if (subscriber.Name is not null)
        {
            entries.Add(new HashEntry(NameField, subscriber.Name));
        }

        if (subscriber.FollowedLeagueId is { } league)
        {
            entries.Add(new HashEntry(LeagueIdField, league.Value));
        }
        else
        {
            await _db.HashDeleteAsync(key, LeagueIdField);
        }

        if (!await _db.KeyExistsAsync(key))
        {
            entries.Add(new HashEntry(CreatedField, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }

        await _db.HashSetAsync(key, [.. entries]);
        await _db.SortedSetAddAsync(IndexKey, subscriber.Id.Value,
            (await _db.HashGetAsync(key, CreatedField)) is { HasValue: true } c ? (double)c : 0);

        foreach (var removed in ParseEvents(existing).Except(subscriber.Events.Current))
        {
            await _db.SetRemoveAsync(EventIndexKey(removed), subscriber.Id.Value);
        }

        foreach (var added in subscriber.Events.Current)
        {
            await _db.SetAddAsync(EventIndexKey(added), subscriber.Id.Value);
        }
    }

    public async Task Delete(WebPushSubscriberId id)
    {
        var existing = await _db.HashGetAsync(SubscriberKey(id), SubsField);
        foreach (var fplEvent in ParseEvents(existing))
        {
            await _db.SetRemoveAsync(EventIndexKey(fplEvent), id.Value);
        }

        await _db.SortedSetRemoveAsync(IndexKey, id.Value);
        await _db.KeyDeleteAsync(SubscriberKey(id));
    }

    public async Task<IEnumerable<WebPushSubscriberId>> GetSubscribedTo(params FplEvent[] fplEvents)
    {
        var keys = fplEvents.Select(e => (RedisKey)EventIndexKey(e)).ToArray();
        var members = await _db.SetCombineAsync(SetOperation.Union, keys);
        return members.Select(m => new WebPushSubscriberId(m.ToString()));
    }

    public async Task<IEnumerable<(WebPushSubscriberId Id, ClassicLeagueId LeagueId)>> GetFollowingALeague(
        params FplEvent[] fplEvents)
    {
        var ids = (await GetSubscribedTo(fplEvents)).ToList();
        var leagues = await Task.WhenAll(ids.Select(id => _db.HashGetAsync(SubscriberKey(id), LeagueIdField)));

        return ids.Zip(leagues)
            .Where(pair => pair.Second.HasValue)
            .Select(pair => (pair.First, new ClassicLeagueId((long)pair.Second)))
            .ToList();
    }

    public async Task<(IReadOnlyList<WebPushSubscriber> Items, int TotalCount)> GetPage(int page, int pageSize)
    {
        var total = (int)await _db.SortedSetLengthAsync(IndexKey);
        var start = page * pageSize;
        var ids = await _db.SortedSetRangeByRankAsync(IndexKey, start, start + pageSize - 1, Order.Descending);

        var items = new List<WebPushSubscriber>();
        foreach (var id in ids)
        {
            if (await Find(new WebPushSubscriberId(id.ToString())) is { } subscriber)
            {
                items.Add(subscriber);
            }
        }

        return (items, total);
    }

    private static string SubscriberKey(WebPushSubscriberId id) => $"WebPushSub-{id.Value}";

    private static string EventIndexKey(FplEvent fplEvent) => $"WebPushSubEvent-{fplEvent}";

    private static IEnumerable<FplEvent> ParseEvents(RedisValue stored) =>
        !stored.HasValue
            ? []
            : stored.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Enum.TryParse<FplEvent>(s, out var parsed) ? parsed : (FplEvent?)null)
                .Where(e => e.HasValue)
                .Select(e => e!.Value);
}
