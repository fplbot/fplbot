using FplBot.Data.Slack;

namespace FplBot.EventHandlers.Slack.Helpers;

public static class SlackTeamExtensions
{
    public static bool HasRegisteredFor(this SlackTeam team, EventSubscription subscription)
    {
        return team.HasChannelAndLeagueSetup() && team.Subscriptions.ContainsSubscriptionFor(subscription);
    }


}
