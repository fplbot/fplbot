namespace FplBot.Core.Taunts
{
    public interface ITaunt
    {
        public TauntType Type { get; }
        public string[] JokePool { get; }

    }

    public enum TauntType
    {
        HasPlayerInTeam,
        InTransfers,
        OutTransfers
    }
}