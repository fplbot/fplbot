using FplBot.Data.Slack;
using FplBot.Services.WebApi.Slack.Abstractions;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Services.WebApi.Slack.Helpers;

internal class SlackWorkSpacePublisher(
    ISlackTeamRepository repository,
    ISlackClientBuilder builder,
    ILogger<SlackWorkSpacePublisher> logger)
    : ISlackWorkSpacePublisher
{
    public async Task PublishToAllWorkspaceChannels(string msg)
    {
        var installations = await repository.GetAllInstallations();
        foreach (var installation in installations)
        {
            foreach (var channel in installation.ChannelSubscriptions)
            {
                await PublishToWorkspace(installation.Id, channel.ChannelId, msg);
            }
        }
    }

    public async Task PublishToWorkspace(string teamId, string channel, params string[] messages)
    {
        foreach (var msg in messages)
        {
            var req = new ChatPostMessageRequest
            {
                Channel = channel,
                Text = msg,
                unfurl_links = "false"
            };
            await PublishToWorkspace(teamId, req);
        }
    }

    public async Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] messages)
    {
        var installation = await repository.GetInstallation(teamId);
        await PublishUsingToken(installation.Token ?? "",messages);
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
                    logger.LogWarning(sae, "Inactive token!");
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
