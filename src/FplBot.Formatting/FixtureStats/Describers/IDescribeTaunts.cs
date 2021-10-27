namespace FplBot.Formatting.FixtureStats
{
    internal interface IDescribeTaunts : IDescribeEvents
    {
        public TauntType Type { get; }
        public string[] JokePool { get; }
    }
}
