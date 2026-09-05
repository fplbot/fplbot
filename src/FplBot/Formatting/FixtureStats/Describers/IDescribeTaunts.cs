using FplBot.Formatting.FixtureStats.Formatters;

namespace FplBot.Formatting.FixtureStats.Describers;

internal interface IDescribeTaunts : IDescribeEvents
{
    public TauntType Type { get; }
    public string[] JokePool { get; }
}
