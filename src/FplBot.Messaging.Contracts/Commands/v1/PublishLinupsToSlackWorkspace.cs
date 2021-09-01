using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public class PublishLineupsToSlackWorkspace : ICommand
    {
        public string WorkspaceId { get; set; }
        public Lineups Lineups { get; set; }
    }
}
