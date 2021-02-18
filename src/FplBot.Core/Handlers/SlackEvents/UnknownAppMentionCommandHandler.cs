using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Commands.v1;
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
            await _session.SendLocal(new PublishToSlack("T0A9QSU83", "#fplbot-notifications", $"Unhandled app_mention:\n * [{eventMetadata.Team_Id}-{slackEvent.User}] \"{slackEvent.Text}\""));
            return new EventHandledResponse("OK");
        }
    }
}