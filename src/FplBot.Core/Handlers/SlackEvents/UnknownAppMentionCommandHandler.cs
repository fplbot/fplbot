using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Core.Handlers.SlackEvents
{
    public class UnknownAppMentionCommandHandler : INoOpAppMentions
    {
        private readonly IMessageSession _session;

        public UnknownAppMentionCommandHandler(IMessageSession session)
        {
            _session = session;
        }
        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
        {
            await _session.Publish(new UnknownAppMentionReceived { Team_Id = eventMetadata.Team_Id, User = slackEvent.User, Text = slackEvent.Text});
            return new EventHandledResponse("OK");
        }
    }
}
