using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Endpoints;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Handlers;

namespace FplBot.WebApi
{
    public class RtmBridgeEventsHandler
    {
        private readonly ILogger<RtmBridgeEventsHandler> _logger;
        private readonly IServiceProvider _provider;

        public RtmBridgeEventsHandler(ILogger<RtmBridgeEventsHandler> logger, IServiceProvider provider)
        {
            _logger = logger;
            _provider = provider;
        }
        
        public async Task Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            await HandleAsRtmMessage(eventMetadata, slackEvent);
        }

        // Converting this to the RTM api message as an interim hack to re-use existing handlers 
        // until we have migrated handler logic to Event payloads
        private async Task HandleAsRtmMessage(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var allHandlers = _provider.GetServices<IHandleMessages>();
            var service = _provider.GetService<ISlackClientService>();
            var slackClient = await service.CreateClient(eventMetadata.Team_Id);
            var helpHandler = new HelpHandler(allHandlers, slackClient);
            var msg = EventParser.ToBackCompatRtmMessage(eventMetadata, slackEvent);

            if (helpHandler.ShouldHandle(msg))
            {
                await helpHandler.Handle(msg);
            }

            var handlers = SelectHandler(allHandlers, msg);
            foreach (var handler in handlers)
            {
                try
                {
                    await handler.Handle(msg);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }

        }

        private IEnumerable<IHandleMessages> SelectHandler(IEnumerable<IHandleMessages> handlers, SlackMessage message)
        {
            var matchingHandlers = handlers.Where(s => s.ShouldHandle(message));
            foreach (var handler in matchingHandlers)
            {
                yield return handler;
            }

            yield return new NoOpHandler();
        }
    }
}