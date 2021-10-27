namespace FplBot.Formatting.FixtureStats
{
    internal class NoOpDescriber : IDescribeEvents
    {
        public string EventDescriptionSingular => "";
        public string EventDescriptionPlural { get; }
        public string EventEmoji => "";
    }
}
