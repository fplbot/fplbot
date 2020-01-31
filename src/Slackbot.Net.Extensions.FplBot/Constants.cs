namespace Slackbot.Net.Extensions.FplBot
{
    internal static class Constants
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
        }

        public static class CronPatterns
        {
            public const string EveryMinute = "0 */1 * * * *";
        }
    }
}
