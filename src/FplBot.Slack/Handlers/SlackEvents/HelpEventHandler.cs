using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FplBot.Slack.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Slack.Handlers.SlackEvents
{
    public class HelpEventHandler : IShortcutAppMentions
    {
        private readonly IEnumerable<IHandleAppMentions> _handlers;
        private readonly ISlackClientBuilder _slackClientService;
        private readonly ISlackTeamRepository _tokenStore;
        private readonly ILogger<HelpEventHandler> _logger;

        public HelpEventHandler(IEnumerable<IHandleAppMentions> allHandlers, ISlackClientBuilder slackClientService, ISlackTeamRepository tokenStore, ILogger<HelpEventHandler> logger)
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

            var team = await _tokenStore.GetTeam(eventMetadata.Team_Id);
            var slackClient = _slackClientService.Build(team.AccessToken);
            await slackClient.ChatPostMessage(@event.Channel, text);
        }

        public bool ShouldShortcut(AppMentionEvent @event)=> @event.Text.Contains("help");
    }
}
