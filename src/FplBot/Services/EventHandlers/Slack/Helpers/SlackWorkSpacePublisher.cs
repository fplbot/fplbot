using FplBot.Data.Slack;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Helpers;

public class SlackWorkSpacePublisher(
    ISlackTeamRepository repository,
    ISlackClientBuilder builder,
    ILogger<SlackWorkSpacePublisher> logger)
    : ISlackWorkSpacePublisher
{
    public async Task PublishToAllWorkspaceChannels(string msg)
    {
        var teams = await repository.GetAllTeams();
        foreach (var team in teams)
        {
            await PublishToWorkspace(team.TeamId!, team.FplBotSlackChannel!, msg);
        }
    }

    public async Task PublishToWorkspace(string teamId, string channel, params string[] messages)
    {
        foreach (var msg in messages)
        {
            if (msg is { Length: > 0 })
            {
                var req = new ChatPostMessageRequest { Channel = channel, Text = msg, unfurl_links = "false" };
                await PublishToWorkspace(teamId, req);
            }
        }
    }

    public async Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] messages)
    {
        var team = await repository.GetTeam(teamId);
        if (team.AccessToken is not null)
        {
            await PublishUsingToken(team.AccessToken,messages);
        }
        else
        {
            logger.LogWarning("Slack Workspace '{TeamId}' is missing a token. Not publishing. ", teamId);
        }

    }

    private async Task PublishUsingToken(string token, params ChatPostMessageRequest[] messages)
    {
        var slackClient = builder.Build(token);
        foreach (var message in messages)
        {
            try
            {
                var res = await slackClient.ChatPostMessage(message);

                if (!res.Ok)
                {
                    logger.LogWarning($"Could not post to {message.Channel}. {res.Error}");
                }
            }
            catch (WellKnownSlackApiException sae)
            {
                if (sae.Error == "account_inactive")
                {
                    logger.LogWarning(sae, $"Inactive token!");
                }
                else
                {
                    logger.LogWarning(sae, $"Could not post to {message.Channel}. {sae.Error} {sae.ResponseContent}") ;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, e.Message);
            }
        }
    }
}
