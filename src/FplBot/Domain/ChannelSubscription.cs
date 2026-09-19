namespace FplBot.Domain;

public class ChannelSubscription
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan MaxFailureAge = TimeSpan.FromDays(7);

    public SubscriptionId Id { get; }

    public string ChannelId { get; private set; }

    public ClassicLeagueId? FollowedLeagueId { get; private set; }

    public int FailureCount { get; private set; }

    public DateTimeOffset? FailingSince { get; private set; }

    public string? LastFailureReason { get; private set; }

    public EventCollection Events { get; } = EventCollection.Empty();

    private ChannelSubscription(SubscriptionId id, string channelId)
    {
        Id = id;
        ChannelId = channelId;
    }

    public static ChannelSubscription Follow(string channelId, ClassicLeagueId leagueId)
    {
        var subscription = new ChannelSubscription(SubscriptionId.New(), channelId);
        subscription.Follow(leagueId);
        return subscription;
    }

    public static ChannelSubscription Subscribe(string channelId, FplEvent[] fplEvents)
    {
        var subscription = new ChannelSubscription(SubscriptionId.New(), channelId);
        subscription.Subscribe(fplEvents);
        return subscription;
    }

    public static ChannelSubscription Load(SubscriptionId id, string channelId, ClassicLeagueId? followedLeagueId, IEnumerable<FplEvent> events,
        int failureCount = 0, DateTimeOffset? failingSince = null, string? lastFailureReason = null)
    {
        var subscription = new ChannelSubscription(id, channelId)
        {
            FollowedLeagueId = followedLeagueId,
            FailureCount = failureCount,
            FailingSince = failingSince,
            LastFailureReason = lastFailureReason
        };
        subscription.Events.Add(events);
        return subscription;
    }

    public void MoveTo(string newChannelId)
    {
        ChannelId = newChannelId;
    }

    public void Follow(ClassicLeagueId leagueId)
    {
        FollowedLeagueId = leagueId;
        if (!Events.Current.Any())
        {
            Events.Add(FplEvent.All);
        }
    }

    public void Unfollow()
    {
        FollowedLeagueId = null;
        Events.Remove(FplEvents.RequiringALeague);
    }

    public void Subscribe(FplEvent fplEvent)
    {
        Events.Add(fplEvent);
    }

    public void Subscribe(FplEvent[] fplEvents)
    {
        Events.Add(fplEvents);
    }

    public void Unsubscribe(FplEvent fplEvent)
    {
        Events.Remove(fplEvent);
    }

    public void Unsubscribe(FplEvent[] fplEvent)
    {
        Events.Remove(fplEvent);
    }

    public bool IsSubscribedTo(FplEvent fplEvent)
    {
        return Events.Contains(fplEvent);
    }

    public void RecordDeliveryFailure(DateTimeOffset now, string reason)
    {
        FailingSince ??= now;
        LastFailureReason = reason;
        FailureCount++;
    }

    public void ClearDeliveryFailures()
    {
        FailingSince = null;
        LastFailureReason = null;
        FailureCount = 0;
    }

    public bool IsStale(DateTimeOffset now) =>
        FailureCount >= MaxFailures && FailingSince is { } since && now - since > MaxFailureAge;
}
