using System.Collections.Generic;
using System.Linq;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public class PlayersPriceChanged : IEvent
    {
        public List<PlayerWithPriceChange> PlayersWithPriceChanges { get; set; }
    }

    public class PlayerWithPriceChange
    {
        public int PlayerId { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public int CostChangeEvent { get; set; }
        public int NowCost { get; set; }
        public double OwnershipPercentage { get; set; }

        public long TeamId { get; set; }
        public string TeamShortName { get; set; }
    }
}
