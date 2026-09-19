using System.Text;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class SubscriptionsCommandHandler(
    ISlackWorkSpacePublisher workspacePublisher,
    ISlackTeamRepository teamRepo,
    ILogger<SubscriptionsCommandHandler> logger)
    : IConsumer<ProcessSubscriptionsCommand>
{
    public async Task Consume(ConsumeContext<ProcessSubscriptionsCommand> context)
    {
        var command = context.Message;
        var subscriptionInfo = await GetCurrentSubscriptions(command);
        await workspacePublisher.PublishToWorkspace(command.TeamId, command.ChannelId, subscriptionInfo);
    }

    private async Task<string> GetCurrentSubscriptions(ProcessSubscriptionsCommand command)
    {
        try
        {
            var installation = await teamRepo.GetInstallation(command.TeamId);
            var currentSubscriptions = installation.GetChannel(command.ChannelId)?.Events.Current ?? [];

            if (currentSubscriptions.Count() < 1)
            {
                return "You are not subscribing to any fplbot updates :disappointed:";
            }

            var sb = new StringBuilder();

            sb.Append("This channel will receive notifications for: \n");

            sb.Append($"{Formatter.BulletPoints(currentSubscriptions)}");

            return sb.ToString();
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, e);
            return "Oops, could not get subscriptions.";
        }
    }
}
