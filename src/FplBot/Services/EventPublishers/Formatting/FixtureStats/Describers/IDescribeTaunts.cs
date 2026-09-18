using FplBot.Formatting.FixtureStats.Formatters;

namespace FplBot.Formatting.FixtureStats.Describers;

internal interface IDescribeTaunts : IDescribeEvents
{
    TauntType Type { get; }
    string[] JokePool { get; }
}
