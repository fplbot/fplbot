using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.SlackClients.Http;

namespace Slackbot.Net.Endpoints
{
    public class HelpEventHandler
    {
        private readonly IEnumerable<IHandleEvent> _handlers;
        private readonly ISlackClient _slackClient;

        public HelpEventHandler(IEnumerable<IHandleEvent> allHandlers, ISlackClient slackClient)
        {
            _handlers = allHandlers;
            _slackClient = slackClient;
        }

        public async Task Handle(EventMetaData eventMetadata, SlackEvent @event)
        {
            var text = _handlers.Where(handler => handler.ShouldShowInHelp)
                .Select(handler => handler.GetHelpDescription())
                .Aggregate("*HALP:*", (current, helpDescription) => current + $"\n• `{helpDescription.Item1}` : _{helpDescription.Item2}_");
            
            var appMention = (AppMentionEvent) @event;

            await _slackClient.ChatPostMessage(appMention.Channel, text);        
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