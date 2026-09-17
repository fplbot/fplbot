using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http.Exceptions;
using Slackbot.Net.SlackClients.Http.Models.Responses.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Helpers;

public static class SlackDelivery
{
    public static async Task<ChatPostMessageResponse?> Post(
        ConsumeContext context,
        IDomainRepository repository,
        string teamId,
        string channelId,
        ILogger logger,
        Func<Task<ChatPostMessageResponse>> post)
    {
        try
        {
            var res = await post();
            if (res.Ok)
            {
                await StaleChannelSubscriptions.ClearFailures(repository, teamId, channelId, logger);
                return res;
            }

            logger.LogWarning("Could not post to {ChannelId}. {Error}", channelId, res.Error);
            await RecordFailure(context, teamId, channelId, res.Error, logger);
            return null;
        }
        catch (WellKnownSlackApiException sae)
        {
            if (sae.Error == "account_inactive")
            {
                logger.LogWarning(sae, "Inactive token!");
            }
            else
            {
                logger.LogWarning(sae, "Could not post to {ChannelId}. {Error} {ResponseContent}", channelId,
                    sae.Error, sae.ResponseContent);
                await RecordFailure(context, teamId, channelId, sae.Error, logger);
            }

            return null;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Delivery to Slack channel {ChannelId} failed, not counted", channelId);
            return null;
        }
    }

    private static async Task RecordFailure(
        ConsumeContext context,
        string teamId,
        string channelId,
        string? slackError,
        ILogger logger)
    {
        if (slackError is null || DeliveryFailureClassifier.Classify(slackError) is not { } reason)
        {
            logger.LogWarning("Delivery to Slack channel {ChannelId} failed, not counted", channelId);
            return;
        }

        await context.Publish(new SlackChannelDeliveryFailed(teamId, channelId, reason, DateTimeOffset.UtcNow));
    }
}
