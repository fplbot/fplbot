using System.Collections.Generic;

namespace Slackbot.Net.Extensions.FplBot
{
    public class PlayerEvent
    {
        public int PlayerId { get; }

        public string PlayerName { get; }

        public TeamType Team { get; }

        public bool IsRemoved { get; }

        public PlayerEvent(int playerId, string playerName, TeamType team, bool isRemoved)
        {
            PlayerId = playerId;
            PlayerName = playerName;
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
