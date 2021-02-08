using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class DebugHandler : IHandleAppMentions
    {
        private readonly IMessageSession _session;

        public DebugHandler(IMessageSession session)
        {
            _session = session;
        }
        
        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
        {
            await _session.SendLocal(new ReplyDebugInfo(eventMetadata.Team_Id, slackEvent.Channel));
            return new EventHandledResponse("OK");
        }

        public bool ShouldHandle(AppMentionEvent slackEvent) => slackEvent.Text.Contains("debug");
    }
}