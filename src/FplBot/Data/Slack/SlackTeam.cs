namespace FplBot.Data.Slack;

public class SlackTeam
{
    public string? TeamId { get; set; }
    public string TeamName { get; set; } = null!;
    public string? Scope { get; set; }
    public string? AccessToken { get; set; }
    public string? FplBotSlackChannel { get; set; }
    public int? FplbotLeagueId { get; set; }
    public bool? PendingRemoval { get; set; }

    public bool HasChannelAndLeagueSetup()
    {
        return !string.IsNullOrEmpty(FplBotSlackChannel) && FplbotLeagueId.HasValue;
    }

    /// <summary>
    /// WIP
    /// </summary>
    public IEnumerable<EventSubscription> Subscriptions { get; set; } = new List<EventSubscription>();
}


public class SlackTeamV2
{
    public string? TeamId { get; set; }
    public string TeamName { get; set; } = null!;
    public string? Scope { get; set; }
    public string? AccessToken { get; set; }
    public bool? PendingRemoval { get; set; }
}

public record SlackTeamV2Subscription(string TeamId,
    string ChannelId,
    int? LeagueId,
    IEnumerable<EventSubscription> Subscriptions
    );
