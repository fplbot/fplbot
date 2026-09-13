namespace FplBot.Data.Slack;

public class SlackTeam
{
    public required string TeamId { get; init; }
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


public record SlackChannelSubscriptionRecord(string TeamId,
    string ChannelId,
    int? LeagueId,
    IEnumerable<EventSubscription> Subscriptions
    );
