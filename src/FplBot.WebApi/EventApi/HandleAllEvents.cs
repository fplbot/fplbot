using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FplBot.WebApi.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Handlers;

namespace FplBot.WebApi.EventApi
{
    public class HandleAllEvents : IHandleAllEvents
    {
        private readonly ILogger<HandleAllEvents> _logger;
        private readonly IServiceProvider _provider;
        private ISlackTeamRepository _slackTeamRepository;

        public HandleAllEvents(ILogger<HandleAllEvents> logger, IServiceProvider provider, ISlackTeamRepository slackTeamRepository)
        {
            _logger = logger;
            _provider = provider;
            _slackTeamRepository = slackTeamRepository;
        }
        
        public async Task Handle(EventMetaData eventMetadata, JObject slackEvent)
        {
            var eventType = EventParser.GetEventType(slackEvent);

            if(eventType == EventTypes.AppUninstalled || eventType == EventTypes.TokensRevoked) 
            {
                await _slackTeamRepository.DeleteByTeamId(eventMetadata.Team_Id);
            }
            else
            {
                await HandleAsRtmMessage(eventMetadata, slackEvent);
            }
        }

        // Converting this to the RTM api message as an interim hack to re-use existing handlers 
        // until we have migrated handler logic to Event payloads
        private async Task HandleAsRtmMessage(EventMetaData eventMetadata, JObject slackEvent)
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