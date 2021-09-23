namespace FplBot.Core.Helpers
{
    internal interface IFormatWithTaunts : IFormatEvents
    {
        public TauntType Type { get; }
        public string[] JokePool { get; }

    }
}
