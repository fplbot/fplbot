namespace FplBot.Formatting.FixtureStats
{
    internal interface IDescribeEvents
    {
        string EventDescriptionSingular { get; }
        string EventDescriptionPlural { get; }
        string EventEmoji { get; }
    }
}
