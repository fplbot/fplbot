using FplBot.Core.Abstractions;

namespace FplBot.Core.Models
{
    public enum StatType
    {
        GoalsScored,
        Assists,
        OwnGoals,
        YellowCards,
        RedCards,
        PenaltiesSaved,
        PenaltiesMissed,
        Unknown
    }

    static class StatTypeMethods
    {
        public static StatType FromStatString(string identifier)
        {
            switch (identifier)
            {
                case "goals_scored":
                    return StatType.GoalsScored;
                case "assists":
                    return StatType.Assists;
                case "own_goals":
                    return StatType.OwnGoals;
                case "yellow_cards":
                    return StatType.YellowCards;
                case "red_cards":
                    return StatType.RedCards;
                case "penalties_saved":
                    return StatType.PenaltiesSaved;
                case "penalties_missed":
                    return StatType.PenaltiesMissed;
                default:
                    return StatType.Unknown;
            }
        }

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