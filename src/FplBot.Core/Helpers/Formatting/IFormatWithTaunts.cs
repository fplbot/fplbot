namespace FplBot.Core.Helpers
{
    internal interface IFormatWithTaunts : IFormatEvents
    {
        string EventDescription { get; }
        string EventEmoji { get; }
        public TauntType Type { get; }
        public string[] JokePool { get; }

    }
}
