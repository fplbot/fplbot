namespace FplBot.Domain;

public class SlackChannelSubscription
{
    public string ChannelId { get; }

    public ClassicLeagueId? FollowedLeagueId { get; private set; }

    public EventCollection Events { get; } = EventCollection.Empty();

    private SlackChannelSubscription(string channelId)
    {
        ChannelId = channelId;
    }

    public static SlackChannelSubscription Follow(string channelId, ClassicLeagueId leagueId)
    {
        var subscription = new SlackChannelSubscription(channelId);
        subscription.Follow(leagueId);
        return subscription;
    }

    public static SlackChannelSubscription Subscribe(string channelId, FplEvent fplEvent)
    {
        var subscription = new SlackChannelSubscription(channelId);
        subscription.Subscribe(fplEvent);
        return subscription;
    }

    public static SlackChannelSubscription Subscribe(string channelId, FplEvent[] fplEvents)
    {
        var subscription = new SlackChannelSubscription(channelId);
        subscription.Subscribe(fplEvents);
        return subscription;
    }

    public void Follow(ClassicLeagueId leagueId)
    {
        FollowedLeagueId = leagueId;
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

    public bool IsSubscribedTo(FplEvent fplEvent)
    {
        return Events.Contains(fplEvent);
    }
}
