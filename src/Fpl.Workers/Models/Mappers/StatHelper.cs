using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers
{
    public static class StatHelper
    {
        public static StatType FromStatString(string identifier)
        {
            return identifier switch
            {
                "goals_scored" => StatType.GoalsScored,
                "assists" => StatType.Assists,
                "own_goals" => StatType.OwnGoals,
                "yellow_cards" => StatType.YellowCards,
                "red_cards" => StatType.RedCards,
                "penalties_saved" => StatType.PenaltiesSaved,
                "penalties_missed" => StatType.PenaltiesMissed,
                _ => StatType.Unknown
            };
        }
    }
}
