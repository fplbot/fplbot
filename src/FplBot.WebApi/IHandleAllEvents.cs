using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Handlers;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Controllers
{
    public interface IHandleAllEvents
    {
        Task Handle(EventWrapper eventWrapper);
    }

    class HandleAllEvents : IHandleAllEvents
    {
        private readonly ILogger<HandleAllEvents> _logger;
        private readonly ISlackClientService _client;
        private readonly IServiceProvider _provider;

        public HandleAllEvents(ILogger<HandleAllEvents> logger, ISlackClientService client, IServiceProvider provider)
        {
            _logger = logger;
            _client = client;
            _provider = provider;
        }
        
        public async Task Handle(EventWrapper eventWrapper)
        {
            _logger.LogInformation("PAYLOAD RECEIVED FROM EVENT API");
            var client = await _client.CreateClient(eventWrapper.Team_Id);
            var allHandlers = _provider.GetServices<IHandleMessages>();
            var msg = new SlackMessage
            {
                Bot = new BotDetails()
                {
                  Id  = "we-dont-get-this-value-from-the-event-api-and-it-changes-between-installs-in-different-workspaces",
                  Name = "we-dont-get-this-value-from-the-event-api"
                },
                Text = eventWrapper.Event.Text,
                MentionsBot = true,
                ChatHub = new ChatHub
                {
                    Id = eventWrapper.Event.Channel,
                    Name = "we-dont-get-this-value-from-the-event-api"
                },
                Team = new TeamDetails
                {
                    Id = eventWrapper.Team_Id,
                    Name = "we-dont-get-this-value-from-the-event-api"
                }
            };
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