using System.Collections.Generic;
using System.Linq;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public class NewPlayersRegistered : IEvent
    {
        public List<NewPlayer> NewPlayers { get; }

        public NewPlayersRegistered(IEnumerable<NewPlayer> newPlayers)
        {
            NewPlayers = newPlayers.ToList();
        }
    }

    public class NewPlayer
    {
        public int PlayerId { get; set; }
        public string WebName { get; set; }
        public int NowCost { get; set; }

        public long TeamId { get; set; }
        public string TeamShortName { get; set; }
    }
}
