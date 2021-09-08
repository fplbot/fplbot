using NServiceBus;

namespace FplBot.Messaging.Contracts.Events.v1
{
    public class UnknownAppMentionReceived : IEvent
    {
        public string Team_Id { get; set; }
        public string User { get; set; }
        public string Text { get; set; }
    }
}
