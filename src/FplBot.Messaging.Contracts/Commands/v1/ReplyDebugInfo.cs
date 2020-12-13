
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public record ReplyDebugInfo(string TeamId, string Channel) : ICommand;
}