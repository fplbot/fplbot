using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public record UnknownAppMentionReceived(string Team_Id, string User, string Text) : IEvent;
}
