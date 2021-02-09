namespace FplBot.Core.Models
{
    public class GameScore
    {
        public int HomeTeamId { get; set; }

        public int AwayTeamId { get; set; }

        public int? HomeTeamScore { get; set; }

        public int? AwayTeamScore { get; set; }
    }
}
