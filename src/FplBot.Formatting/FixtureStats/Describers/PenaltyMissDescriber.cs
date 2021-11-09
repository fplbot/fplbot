using FplBot.Formatting.FixtureStats.Formatters;

namespace FplBot.Formatting.FixtureStats.Describers;

public class PenaltyMissDescriber : IDescribeTaunts
{
    public TauntType Type => TauntType.HasPlayerInTeam;

    public string[] JokePool => new []
    {
        "Bet you thought you were getting some points there, {0}!",
        "Isn't that guy in your team, {0}?"
    };

    public string EventDescriptionSingular => "{0} missed a penalty! {1}";
    public string EventDescriptionPlural => "{0} missed {1} penalties! {2}";
    public string EventEmoji => "🥴";
}