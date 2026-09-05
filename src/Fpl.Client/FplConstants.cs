namespace Fpl.Client;

public class FplConstants
{
    public static class ChipNames
    {
        public const string TripleCaptain = "3xc";
        public const string Wildcard = "wildcard";
        public const string FreeHit = "freehit";
        public const string BenchBoost = "bboost";
    }

    public static class StatIdentifiers
    {
        public const string GoalsScored = "goals_scored";
        public const string Assists = "assists";
        public const string OwnGoals = "own_goals";
        public const string YellowCards = "yellow_cards";
        public const string RedCards = "red_cards";
        public const string PenaltiesSaved = "penalties_saved";
        public const string PenaltiesMissed = "penalties_missed";
        public const string Bps = "bps";
        public const string DefensiveContribution = "defensive_contribution";
    }

    // https://www.premierleague.com/news/4324847
    // Defenders earn defensive contribution points for 10+ clearances, blocks, interceptions and tackles.
    // Midfielders and forwards earn them for 12+ clearances, blocks, interceptions, tackles and recoveries.
    // Goalkeepers do not earn defensive contribution points.
    public static class DefensiveContributionThresholds
    {
        public const int Defender = 10;
        public const int MidfielderAndForward = 12;
    }
}
