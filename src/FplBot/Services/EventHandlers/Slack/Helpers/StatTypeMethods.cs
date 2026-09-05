using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.EventHandlers.Slack.Helpers;

public static class StatTypeMethods
{
    public static EventSubscription? GetSubscriptionType(this StatType statType)
    {
        return statType switch
        {
            StatType.GoalsScored => EventSubscription.FixtureGoals,
            StatType.Assists => EventSubscription.FixtureAssists,
            StatType.OwnGoals => EventSubscription.FixtureGoals,
            StatType.RedCards => EventSubscription.FixtureCards,
            StatType.PenaltiesSaved => EventSubscription.FixturePenaltyMisses,
            StatType.PenaltiesMissed => EventSubscription.FixturePenaltyMisses,
            _ => null
        };
    }
}
