using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatPostMessage;

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
                await PublishToWorkspace(installation.ExternalId, channel.ChannelId, msg);
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
        if (installation.Token is null)
        {
            logger.LogWarning("Slack Workspace '{TeamId}' is missing a token. Not publishing. ", teamId);
            return;
        }

        foreach (var message in messages)
        {
            await PublishUsingToken(installation, message);
        }
    }

    public async Task<ChatPostMessageResponse?> PublishToWorkspaceWithResponse(string teamId, ChatPostMessageRequest message)
    {
        var installation = await repository.GetInstallation(teamId);
        if (installation.Token is null)
        {
            logger.LogWarning("Slack Workspace '{TeamId}' is missing a token. Not publishing. ", teamId);
            return null;
        }

        return await PublishUsingToken(installation, message);
    }

    private async Task<ChatPostMessageResponse?> PublishUsingToken(Installation installation, ChatPostMessageRequest message)
    {
        var slackClient = builder.Build(installation.Token!);
        try
        {
            var res = await slackClient.ChatPostMessage(message);

            if (res.Ok)
            {
                await ClearFailures(installation, message.Channel);
                return res;
            }

            logger.LogWarning("Could not post to {ChannelId}. {Error}", message.Channel, res.Error);
            await RecordFailure(installation.ExternalId, message.Channel, res.Error);
            return null;
        }
        catch (WellKnownSlackApiException sae)
        {
            logger.LogWarning(sae, "Could not post to {ChannelId}. {Error} {ResponseContent}", message.Channel,
                sae.Error, sae.ResponseContent);
            await RecordFailure(installation.ExternalId, message.Channel, sae.Error);
            return null;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, e.Message);
            return null;
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
        await repository.SaveChannelSubscription(installation.ExternalId, subscription);
    }

    private async Task RecordFailure(string teamId, string channelId, string? slackError)
    {
        if (slackError is null || DeliveryFailureClassifier.Classify(slackError) is not { } reason)
        {
            logger.LogWarning("Delivery to Slack channel {ChannelId} failed, not counted", channelId);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
            .Publish(new SlackChannelDeliveryFailed(teamId, channelId, reason, DateTimeOffset.UtcNow));
    }
}
