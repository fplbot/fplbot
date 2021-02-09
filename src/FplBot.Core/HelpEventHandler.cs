using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Core
{
    public class HelpEventHandler : IShortcutAppMentions
    {
        private readonly IEnumerable<IHandleAppMentions> _handlers;
        private readonly ISlackClientBuilder _slackClientService;
        private readonly ITokenStore _tokenStore;
        private readonly ILogger<HelpEventHandler> _logger;

        public HelpEventHandler(IEnumerable<IHandleAppMentions> allHandlers, ISlackClientBuilder slackClientService, ITokenStore tokenStore, ILogger<HelpEventHandler> logger)
        {
            _handlers = allHandlers;
            _slackClientService = slackClientService;
            _tokenStore = tokenStore;
            _logger = logger;
        }

        public async Task Handle(EventMetaData eventMetadata, AppMentionEvent @event)
        {
            var text = _handlers.Select(handler => handler.GetHelpDescription())
                .Where(desc => !string.IsNullOrEmpty(desc.HandlerTrigger))
                .Aggregate("*HALP:*", (current, tuple) => current + $"\n• `{tuple.HandlerTrigger}` : _{tuple.Description}_");

            var token = await _tokenStore.GetTokenByTeamId(eventMetadata.Team_Id);
            var slackClient = _slackClientService.Build(token);
            await slackClient.ChatPostMessage(@event.Channel, text);
        }

        public bool ShouldShortcut(AppMentionEvent @event)=> @event.Text.Contains("help");
    }
}