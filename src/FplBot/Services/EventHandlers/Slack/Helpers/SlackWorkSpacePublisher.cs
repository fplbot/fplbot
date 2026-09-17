using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Helpers;

public class SlackWorkSpacePublisher(
    ISlackTeamRepository repository,
    ISlackClientBuilder builder,
    IServiceScopeFactory scopeFactory,
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
            if (msg is { Length: > 0 })
            {
                var req = new ChatPostMessageRequest { Channel = channel, Text = msg, unfurl_links = "false" };
                await PublishToWorkspace(teamId, req);
            }
        }
    }

    public async Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] messages)
    {
        var installation = await repository.GetInstallation(teamId);
        if (installation.Token is not null)
        {
            await PublishUsingToken(installation, messages);
        }
        else
        {
            logger.LogWarning("Slack Workspace '{TeamId}' is missing a token. Not publishing. ", teamId);
        }

    }

    private async Task PublishUsingToken(Installation installation, params ChatPostMessageRequest[] messages)
    {
        var slackClient = builder.Build(installation.Token!);
        foreach (var message in messages)
        {
            try
            {
                var res = await slackClient.ChatPostMessage(message);

                if (res.Ok)
                {
                    await ClearFailures(installation, message.Channel);
                }
                else
                {
                    logger.LogWarning($"Could not post to {message.Channel}. {res.Error}");
                    await RecordFailure(installation.Id, message.Channel, res.Error);
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
                    await RecordFailure(installation.Id, message.Channel, sae.Error);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, e.Message);
            }
        }
    }

    private async Task ClearFailures(Installation installation, string channelId)
    {
        var subscription = installation.GetChannel(channelId);
        if (subscription is null || subscription.FailureCount == 0)
        {
            return;
        }

        subscription.ClearDeliveryFailures();
        await repository.SaveChannelSubscription(installation.Id, subscription);
    }

    private async Task RecordFailure(string teamId, string channelId, string? slackError)
    {
        if (slackError is null || DeliveryFailureClassifier.Classify(slackError) is not { } reason)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
            .Publish(new SlackChannelDeliveryFailed(teamId, channelId, reason, DateTimeOffset.UtcNow));
    }
}
