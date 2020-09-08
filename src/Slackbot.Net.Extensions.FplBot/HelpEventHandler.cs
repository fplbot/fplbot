using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<HelpEventHandler> _logger;

        public HelpEventHandler(IEnumerable<IHandleEvent> allHandlers, ISlackClientService slackClientService, ILogger<HelpEventHandler> logger)
        {
            _handlers = allHandlers;
            _slackClientService = slackClientService;
            _logger = logger;
        }

        public async Task Handle(EventMetaData eventMetadata, SlackEvent @event)
        {
            switch (@event)
            {
                case UnknownSlackEvent unknown:
                    // do stuff with new events not in Slackbot.Net, i.e. : `app_home_opened` 
                    _logger.LogInformation(unknown.RawJson);
                    break;
                case AppMentionEvent appMention:
                {
                    var text = _handlers.Where(h => !(h is FplBotJoinedChannelHandler) && !(h is AppHomeOpenedEventHandler)).Select(handler => handler.GetHelpDescription())
                        .Aggregate("*HALP:*", (current, helpDescription) => current + $"\n• `{helpDescription.HandlerTrigger}` : _{helpDescription.Description}_");

                    var slackClient = await _slackClientService.CreateClient(eventMetadata.Team_Id);
                    await slackClient.ChatPostMessage(appMention.Channel, text);
                    break;
                }
                default:
                    _logger.LogWarning($"Received unhandled event: {@event.Type}");
                    break;
            }
        }

        public bool ShouldHandle(SlackEvent @event)
        {
            return @event switch
            {
                AppMentionEvent appMention => appMention.Text.Contains("help"),
                UnknownSlackEvent unknown => true,
                _ => false
            };
        }
    }
}