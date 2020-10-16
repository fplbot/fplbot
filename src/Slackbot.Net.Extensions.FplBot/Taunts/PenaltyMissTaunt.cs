namespace Slackbot.Net.Extensions.FplBot.Taunts
{
    public class PenaltyMissTaunt : ITaunt
    {
        public TauntType Type => TauntType.HasPlayerInTeam;

        public string[] JokePool => new []
        {
            "Bet you thought you were getting some points there, {0}!",
            "Isn't that guy in your team, {0}?"
        };
    }
}