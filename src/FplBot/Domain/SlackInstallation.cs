namespace FplBot.Domain;

public class SlackInstallation
{
    public string TeamId { get; }
    public string? Token { get; private set; }

    private readonly List<SlackChannelSubscription> _channelSubscriptions = new();
    public IReadOnlyCollection<SlackChannelSubscription> ChannelSubscriptions => _channelSubscriptions;

    public bool IsActive => Token is not null;

    private SlackInstallation(string teamId, string token)
    {
        TeamId = teamId;
        Token = token;
    }

    public static SlackInstallation Install(string teamId, string token) => new(teamId, token);

    public void Uninstall()
    {
        Token = null;
        _channelSubscriptions.Clear();
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
