namespace FplBot.Core.Taunts
{
    public class GoalTaunt : ITaunt
    {
        public TauntType Type => TauntType.OutTransfers;

        public string[] JokePool => new []
        {
            "Ah jeez, you transferred him out, {0} :joy:",
            "You just had to knee jerk him out, didn't you, {0}?",
            "Didn't you have that guy last week, {0}?",
            "Goddammit, really? You couldn't hold on to him just one more gameweek, {0}?"
        };
    }
}