namespace FplBot.Domain;

public class ChannelSubscription
{
    public string ChannelId { get; }

    public ClassicLeagueId? FollowedLeagueId { get; private set; }

    public EventCollection Events { get; } = EventCollection.Empty();

    private ChannelSubscription(string channelId)
    {
        ChannelId = channelId;
    }

    public static ChannelSubscription Follow(string channelId, ClassicLeagueId leagueId)
    {
        var subscription = new ChannelSubscription(channelId);
        subscription.Follow(leagueId);
        return subscription;
    }

    public static ChannelSubscription Subscribe(string channelId, FplEvent[] fplEvents)
    {
        var subscription = new ChannelSubscription(channelId);
        subscription.Subscribe(fplEvents);
        return subscription;
    }

    public static ChannelSubscription Load(string channelId, ClassicLeagueId? followedLeagueId, IEnumerable<FplEvent> events)
    {
        var subscription = new ChannelSubscription(channelId) { FollowedLeagueId = followedLeagueId };
        subscription.Events.Add(events);
        return subscription;
    }

    public void Follow(ClassicLeagueId leagueId)
    {
        FollowedLeagueId = leagueId;
        if (!Events.Current.Any())
        {
            Events.Add(FplEvent.All);
        }
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
}
