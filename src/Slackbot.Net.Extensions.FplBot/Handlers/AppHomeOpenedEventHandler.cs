using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage.Blocks;
using Slackbot.Net.SlackClients.Http.Models.Requests.ViewPublish;
using Slackbot.Net.SlackClients.Http.Models.Responses.ViewPublish;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class AppHomeOpenedEventHandler : IHandleAppHomeOpened
    {
        private readonly ISlackClientBuilder _builder;
        private readonly ISlackTeamRepository _repo;
        private readonly ILogger<AppHomeOpenedEvent> _logger;

        public AppHomeOpenedEventHandler(ISlackClientBuilder builder, ISlackTeamRepository repo, ILogger<AppHomeOpenedEvent> logger)
        {
            _builder = builder;
            _repo = repo;
            _logger = logger;
        }
        
        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppHomeOpenedEvent appHomeEvent)
        {
            var team = await _repo.GetTeam(eventMetadata.Team_Id);
            var client = _builder.Build(team.AccessToken);
            
            var viewPublishRequest = BuildViewRequest(appHomeEvent.User);
            ViewPublishResponse res = null;
            try
            {
                res = await client.ViewPublish(viewPublishRequest);
                return !res.Ok ? new EventHandledResponse(res.Error) : new EventHandledResponse("Opened AppHome");
            }
            catch (WellKnownSlackApiException se)
            {
                _logger.LogError(se.Message, se);
                return new EventHandledResponse(se.Message);
            }
        }

        private static ViewPublishRequest BuildViewRequest(string userId)
        {
            return new ViewPublishRequest(userId)
            {
                View = new View
                {
                    Type = PublishViewConstants.Home,
                    Blocks = new IBlock[]
                    {
                        new Block
                        {
                            type = BlockTypes.Section,
                            text = new Text
                            {
                                type = TextTypes.PlainText,
                                text = $"Configuration should go here! This view was rendered {DateTime.UtcNow.ToLongDateString()} - {DateTime.UtcNow.ToLongTimeString()}"
                            }
                        }
                    }
                }
            };
        }
    }
}