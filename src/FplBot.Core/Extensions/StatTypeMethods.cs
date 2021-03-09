using FplBot.Data.Models;

namespace FplBot.Core.Models
{
    public static class StatTypeMethods
    {
        public static EventSubscription? GetSubscriptionType(this StatType statType)
        {
            return statType switch
            {
                StatType.GoalsScored => EventSubscription.FixtureGoals,
                StatType.Assists => EventSubscription.FixtureAssists,
                StatType.OwnGoals => EventSubscription.FixtureGoals,
                StatType.YellowCards => EventSubscription.FixtureCards,
                StatType.RedCards => EventSubscription.FixtureCards,
                StatType.PenaltiesSaved => EventSubscription.FixturePenaltyMisses,
                StatType.PenaltiesMissed => EventSubscription.FixturePenaltyMisses,
                _ => null
            };
        }
    }
}
