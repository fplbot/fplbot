namespace FplBot.Domain;

public class SlackInstallation
{
    public string TeamId { get; }
    public string TeamName { get; }
    public string? Token { get; private set; }
    public bool PendingRemoval { get; private set; }

    private readonly List<SlackChannelSubscription> _channelSubscriptions = new();
    public IReadOnlyCollection<SlackChannelSubscription> ChannelSubscriptions => _channelSubscriptions;

    public bool IsActive => Token is not null && !PendingRemoval;

    private SlackInstallation(string teamId, string teamName, string token)
    {
        TeamId = teamId;
        TeamName = teamName;
        Token = token;
    }

    public static SlackInstallation Install(string teamId, string teamName, string token) => new(teamId, teamName, token);

    public static SlackInstallation FromStorage(string teamId, string teamName, string token, IEnumerable<SlackChannelSubscription> channelSubscriptions, bool pendingRemoval = false)
    {
        var installation = new SlackInstallation(teamId, teamName, token) { PendingRemoval = pendingRemoval };
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

        _channelSubscriptions.Add(SlackChannelSubscription.Follow(channelId, leagueId));
    }

    public void Subscribe(string channelId, FplEvent[] fplEvents)
    {
        var existing = FindChannel(channelId);
        if (existing is not null)
        {
            existing.Subscribe(fplEvents);
            return;
        }

        _channelSubscriptions.Add(SlackChannelSubscription.Subscribe(channelId, fplEvents));
    }

    public void Unsubscribe(string channelId, FplEvent fplEvent)
    {
        FindChannel(channelId)?.Unsubscribe(fplEvent);
    }

    public void Unsubscribe(string channelId, FplEvent[] fplEvent)
    {
        FindChannel(channelId)?.Unsubscribe(fplEvent);
    }

    private SlackChannelSubscription? FindChannel(string channelId) =>
        _channelSubscriptions.FirstOrDefault(c => c.ChannelId == channelId);
}
