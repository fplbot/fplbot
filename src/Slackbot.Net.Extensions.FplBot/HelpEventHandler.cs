using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Handlers;

namespace Slackbot.Net.Extensions.FplBot
{
    public class HelpEventHandler : IShortcutHandler
    {
        private readonly IEnumerable<IHandleEvent> _handlers;
        private readonly ISlackClientService _slackClientService;

        public HelpEventHandler(IEnumerable<IHandleEvent> allHandlers, ISlackClientService slackClientService)
        {
            _handlers = allHandlers;
            _slackClientService = slackClientService;
        }

        public async Task Handle(EventMetaData eventMetadata, SlackEvent @event)
        {
            var text = _handlers.Select(handler => handler.GetHelpDescription())
                .Aggregate("*HALP:*", (current, helpDescription) => current + $"\n• `{helpDescription.HandlerTrigger}` : _{helpDescription.Description}_");
            
            var appMention = (AppMentionEvent) @event;

            var slackClient = await _slackClientService.CreateClient(eventMetadata.Team_Id);
            await slackClient.ChatPostMessage(appMention.Channel, text);        
        }

        public bool ShouldHandle(SlackEvent @event)
        {
            if (@event is AppMentionEvent)
            {
                var appMention = (AppMentionEvent) @event;
                return appMention.Text.Contains("help");
            }

            return true;
        }
    }
}