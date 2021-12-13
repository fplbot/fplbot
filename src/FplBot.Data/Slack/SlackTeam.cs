namespace FplBot.Data.Slack;

public class SlackTeam
{
    public SlackTeam()
    {
        Subscriptions = new List<EventSubscription>();
    }

    public string TeamId { get; set; }
    public string TeamName { get; set; }
    public string Scope { get; set; }
    public string AccessToken { get; set; }
    public string FplBotSlackChannel { get; set; }
    public int? FplbotLeagueId { get; set; }

    public bool HasChannelAndLeagueSetup()
    {
        return !string.IsNullOrEmpty(FplBotSlackChannel) && FplbotLeagueId.HasValue;
    }

    /// <summary>
    /// WIP
    /// </summary>
    public IEnumerable<EventSubscription> Subscriptions { get; set; }
}
