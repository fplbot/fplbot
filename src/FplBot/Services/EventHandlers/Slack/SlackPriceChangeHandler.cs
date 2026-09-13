using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class SlackPriceChangeHandler(
    ISlackWorkSpacePublisher publisher,
    ISlackTeamRepository slackTeamRepo,
    ILogger<SlackPriceChangeHandler> logger)
    : IConsumer<PlayersPriceChanged>, IConsumer<PublishPriceChangesToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<PlayersPriceChanged> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.PlayersWithPriceChanges.Count()} price updates");
        var installations = await slackTeamRepo.GetAllInstallations();
        foreach (var installation in installations)
        {
            if (installation.HasRegisteredFor(FplEvent.PriceChanges))
            {
                await context.Publish(new PublishPriceChangesToSlackWorkspace(installation.TeamId, notification.PlayersWithPriceChanges.ToList()));
            }
        }
    }

    public async Task Consume(ConsumeContext<PublishPriceChangesToSlackWorkspace> context)
    {
        var message = context.Message;
        logger.LogInformation($"Publish price changes to {message.WorkspaceId}");
        var filtered = message.PlayersWithPriceChanges.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var installation = await slackTeamRepo.GetInstallation(message.WorkspaceId);
            var formatted = Formatter.FormatPriceChanged(filtered);
            await publisher.PublishToWorkspace(installation.TeamId, installation.PrimaryChannel()!.ChannelId, formatted);
        }
        else
        {
            logger.LogInformation("All price changes were irrelevant, so not sending any notification");
        }
    }
}
