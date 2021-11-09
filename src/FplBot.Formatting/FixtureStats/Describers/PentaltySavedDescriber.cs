using FplBot.Formatting.FixtureStats.Formatters;

namespace FplBot.Formatting.FixtureStats.Describers;

public class PentaltySavedDescriber : IDescribeTaunts
{
    public TauntType Type => TauntType.OutTransfers;

    public string[] JokePool => new []
    {
        "Ah jeez, you transferred him out, {0} 🤣",
        "You just had to knee jerk him out, didn't you, {0}?",
        "Didn't you have that guy last week, {0}?",
        "Goddammit, really? You couldn't hold on to him just one more gameweek, {0}?"
    };

    public string EventDescriptionSingular => "{0} saved a penalty! {1}";
    public string EventDescriptionPlural => "{0} saved {1} penalties! {2}";
    public string EventEmoji => "🤸‍♂️";
}