namespace FplBot.Domain;

public class WebPushSubscriber
{
    public WebPushSubscriberId Id { get; }

    public PushKeys PushKeys { get; }

    public string? Name { get; }

    public ClassicLeagueId? FollowedLeagueId { get; private set; }

    public FplEntryId? LinkedEntryId { get; private set; }

    public EventCollection Events { get; } = EventCollection.Empty();

    private WebPushSubscriber(WebPushSubscriberId id, PushKeys pushKeys, string? name)
    {
        Id = id;
        PushKeys = pushKeys;
        Name = name;
    }

    public static WebPushSubscriber Register(PushKeys pushKeys, string? name, ClassicLeagueId? leagueId, FplEntryId? entryId = null)
    {
        var subscriber = new WebPushSubscriber(WebPushSubscriberId.New(), pushKeys, name);
        if (leagueId is not null)
        {
            subscriber.Follow(leagueId);
        }

        if (entryId is not null)
        {
            subscriber.LinkEntry(entryId);
        }

        subscriber.Subscribe(FplEvents.SupportedOnWeb);
        return subscriber;
    }

    public static WebPushSubscriber Load(WebPushSubscriberId id, PushKeys pushKeys, string? name,
        ClassicLeagueId? followedLeagueId, FplEntryId? linkedEntryId, IEnumerable<FplEvent> events)
    {
        var subscriber = new WebPushSubscriber(id, pushKeys, name)
        {
            FollowedLeagueId = followedLeagueId,
            LinkedEntryId = linkedEntryId
        };
        subscriber.Events.Add(events);
        return subscriber;
    }

    public void Follow(ClassicLeagueId leagueId) => FollowedLeagueId = leagueId;

    public void Unfollow()
    {
        FollowedLeagueId = null;
        Events.Remove(FplEvents.RequiringALeague);
    }

    public void LinkEntry(FplEntryId entryId) => LinkedEntryId = entryId;

    public void UnlinkEntry() => LinkedEntryId = null;

    public void Subscribe(FplEvent[] fplEvents) => Events.Add(Allowed(fplEvents));

    public void Unsubscribe(FplEvent[] fplEvents) => Events.Remove(fplEvents);

    public bool IsSubscribedTo(FplEvent fplEvent) => Events.Contains(fplEvent);

    private IEnumerable<FplEvent> Allowed(IEnumerable<FplEvent> fplEvents) =>
        fplEvents
            .SelectMany(e => e == FplEvent.All ? FplEvents.SupportedOnWeb : [e])
            .Where(e => FplEvents.SupportedOnWeb.Contains(e))
            .Where(e => FollowedLeagueId is not null || !FplEvents.RequiringALeague.Contains(e))
            .Distinct();
}
