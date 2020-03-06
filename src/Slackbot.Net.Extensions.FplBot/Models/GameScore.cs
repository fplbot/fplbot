namespace Slackbot.Net.Extensions.FplBot
{
    public class GameScore
    {
        public int HomeTeamId { get; set; }

        public int AwayTeamId { get; set; }

        public string HomeTeamName { get; set; }

        public string AwayTeamName { get; set; }

        public int? HomeTeamScore { get; set; }

        public int? AwayTeamScore { get; set; }
    }
}
