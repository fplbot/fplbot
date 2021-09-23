namespace FplBot.Core.Helpers
{
    internal interface IFormatEvents : IFormat
    {
        string EventDescriptionSingular { get; }
        string EventDescriptionPlural { get; }

        string EventEmoji { get; }
    }
}
