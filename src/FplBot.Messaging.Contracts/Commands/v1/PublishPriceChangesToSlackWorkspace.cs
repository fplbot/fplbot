using System.Collections.Generic;
using System.Linq;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public class PublishPriceChangesToSlackWorkspace : ICommand
    {
        public string WorkspaceId { get; set; }
        public List<PlayerWithPriceChange> PlayersWithPriceChanges { get; set; }
    }
}
