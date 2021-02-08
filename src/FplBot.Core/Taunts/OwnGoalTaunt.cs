namespace FplBot.Core.Taunts
{
    public class OwnGoalTaunt : ITaunt
    {
        public TauntType Type => TauntType.HasPlayerInTeam;

        public string[] JokePool => new[]
        {
            "Isn't that guy in your team, {0}?",
            "That's -2pts, {0} :grimacing:",
            "Are you playing anti-fpl, {0}?"
        };
    }
}