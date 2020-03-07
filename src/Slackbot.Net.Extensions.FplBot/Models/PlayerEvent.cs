namespace Slackbot.Net.Extensions.FplBot
{
    public class PlayerEvent
    {
        public int PlayerId { get; }

        public TeamType Team { get; }

        public bool IsRemoved { get; }

        public PlayerEvent(int playerId, TeamType team, bool isRemoved)
        {
            PlayerId = playerId;
            Team = team;
            IsRemoved = isRemoved;
        }

        public enum TeamType
        {
            Home,
            Away
        }
    }
}
