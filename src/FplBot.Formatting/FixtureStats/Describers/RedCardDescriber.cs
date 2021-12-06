using FplBot.Formatting.FixtureStats.Formatters;

namespace FplBot.Formatting.FixtureStats.Describers;

public class RedCardDescriber : IDescribeTaunts
{
    public TauntType Type => TauntType.InTransfers;

    public string[] JokePool => new []
    {
        "Smart move bringing him in, {0} 🙃",
        "Didn't you transfer him in this week, {0}? 👹",
        "Maybe you should have waited a couple of more weeks before knee jerking him in, {0}?"
    };

    public string EventDescriptionSingular => "{0} got a red card! {1}";
    public string EventDescriptionPlural => "{0} got {1} red cards!? {2}";

    public string EventEmoji => "🔴";
}
