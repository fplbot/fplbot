namespace FplBot.Formatting.FixtureStats.Describers;

internal interface IDescribeEvents
{
    string EventDescriptionSingular { get; }
    string EventDescriptionPlural { get; }
    string EventEmoji { get; }
}