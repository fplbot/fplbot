using FplBot.Data.Slack;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Models.BlockKit;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ViewPublish;
using Slackbot.Net.SlackClients.Http.Models.Responses.ViewPublish;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

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



        var viewPublishRequest = BuildViewRequest(appHomeEvent.User, team.FplbotLeagueId);
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

    private static ViewPublishRequest BuildViewRequest(string userId, long? leagueId)
    {
        return new ViewPublishRequest(userId)
        {
            View = new View
            {
                Type = PublishViewConstants.Home,
                Blocks = new IBlock[]
                {
                    new SectionBlock
                    {
                        type = BlockTypes.Section,
                        text = new Text
                        {
                            type = TextTypes.PlainText,
                            text = "Welcome to the home!!"
                        }
                    },
                    new DividerBlock()
                    {
                        type = BlockTypes.Divider
                    },
                    new SectionBlock()
                    {
                        type = BlockTypes.Section,
                        text = new Text
                        {
                            type = TextTypes.PlainText,
                            text = "Here you can update your teams FPL League ID"
                        }

                    },
                    new InputBlock
                    {
                        dispatch_action = true,
                        label = new Text
                        {
                            type = TextTypes.PlainText,
                            text = "FPL League Id"

                        },
                        element = new PlainTextInputElement
                        {
                            action_id = "fpl_league_id_action",
                            initial_value = leagueId?.ToString()
                        }

                    }
                }
            }
        };
    }
}
