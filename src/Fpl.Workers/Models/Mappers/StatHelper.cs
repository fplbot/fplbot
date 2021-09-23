using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using FplBot.Core.Models;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers
{
    public static class StatHelper
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
    }
}
