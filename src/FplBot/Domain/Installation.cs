namespace FplBot.Domain;

public class Installation
{
    public string Id { get; }
    public string Name { get; }
    public string? Token { get; private set; }
    public bool PendingRemoval { get; private set; }

    private readonly List<ChannelSubscription> _channelSubscriptions = new();
    public IReadOnlyCollection<ChannelSubscription> ChannelSubscriptions => _channelSubscriptions;

    private Installation(string id, string name, string? token)
    {
        Id = id;
        Name = name;
        Token = token;
    }

    public static Installation Install(string id, string name, string token) => new(id, name, token);

    public static Installation Install(string id, string name) => new(id, name, token: null);

    public static Installation Reinstall(string id, string name, string token, IEnumerable<ChannelSubscription> existingChannels) =>
        Load(id, name, token, existingChannels);

    public static Installation Reinstall(string id, string name, IEnumerable<ChannelSubscription> existingChannels) =>
        Load(id, name, token: null, existingChannels);

    public static Installation Load(string id, string name, string? token, IEnumerable<ChannelSubscription> channelSubscriptions, bool pendingRemoval = false)
    {
        var installation = new Installation(id, name, token) { PendingRemoval = pendingRemoval };
        installation._channelSubscriptions.AddRange(channelSubscriptions);
        return installation;
    }

    public void Uninstall()
    {
        Token = null;
        _channelSubscriptions.Clear();
    }

    public void MarkForRemoval()
    {
        PendingRemoval = true;
    }

    public void Follow(string channelId, ClassicLeagueId leagueId)
    {
        var existing = FindChannel(channelId);
        if (existing is not null)
        {
            existing.Follow(leagueId);
            return;
        }

        _channelSubscriptions.Add(ChannelSubscription.Follow(channelId, leagueId));
    }

    public void Unfollow(string channelId)
    {
        FindChannel(channelId)?.Unfollow();
    }

    public void Subscribe(string channelId, FplEvent[] fplEvents)
    {
        var existing = FindChannel(channelId);
        if (existing is not null)
        {
            existing.Subscribe(fplEvents);
            return;
        }

        _channelSubscriptions.Add(ChannelSubscription.Subscribe(channelId, fplEvents));
    }

    public void Unsubscribe(string channelId, FplEvent fplEvent)
    {
        FindChannel(channelId)?.Unsubscribe(fplEvent);
    }

    public void Unsubscribe(string channelId, FplEvent[] fplEvent)
    {
        FindChannel(channelId)?.Unsubscribe(fplEvent);
    }

    public ChannelSubscription? GetChannel(string channelId) => FindChannel(channelId);

    public void RemoveChannel(string channelId)
    {
        var existing = FindChannel(channelId);
        if (existing is null) return;

        _channelSubscriptions.Remove(existing);
    }

    public MoveChannelOutcome MoveChannel(string oldChannelId, string newChannelId)
    {
        var existing = FindChannel(oldChannelId);
        if (existing is null) return MoveChannelOutcome.SourceNotFound;

        if (FindChannel(newChannelId) is not null) return MoveChannelOutcome.TargetAlreadySubscribed;

        _channelSubscriptions.Remove(existing);
        _channelSubscriptions.Add(ChannelSubscription.Load(newChannelId, existing.FollowedLeagueId, existing.Events.Current));
        return MoveChannelOutcome.Moved;
    }

    private ChannelSubscription? FindChannel(string channelId) =>
        _channelSubscriptions.FirstOrDefault(c => c.ChannelId == channelId);

    public IEnumerable<ChannelSubscription> GetSubscriptionsTo(FplEvent fplEvent)
    {
        return _channelSubscriptions.Where(c => c.IsSubscribedTo(fplEvent));
    }
}
