
using NServiceBus;

namespace FplBot.Messaging.Contracts.Commands.v1
{
    public class PublishToSlack : ICommand
    {
        public PublishToSlack(string TeamId, string Channel, string Message)
        {
            this.TeamId = TeamId;
            this.Channel = Channel;
            this.Message = Message;
        }

        public string TeamId { get; set; }

        public string Channel { get; set; }

        public string Message { get; set; }
    }

    public class PublishSlackThreadMessage : ICommand
    {
        public string TeamId { get; set; }
        public string Channel { get; set; }
        public string Timestamp { get; set; }
        public string Message { get; set; }
    }
}
