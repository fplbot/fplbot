using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class PriceChangeHandler(
    ISlackWorkSpacePublisher publisher,
    ISlackTeamRepository slackTeamRepo,
    ILogger<PriceChangeHandler> logger)
    : IConsumer<PlayersPriceChanged>, IConsumer<PublishPriceChangesToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<PlayersPriceChanged> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.PlayersWithPriceChanges.Count()} price updates");
        var slackTeams = await slackTeamRepo.GetAllTeams();
        foreach (var slackTeam in slackTeams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.PriceChanges))
            {
                await context.Publish(new PublishPriceChangesToSlackWorkspace(slackTeam.TeamId!, notification.PlayersWithPriceChanges.ToList()));
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
            var slackTeam = await slackTeamRepo.GetTeam(message.WorkspaceId);
            var formatted = Formatter.FormatPriceChanged(filtered);
            await publisher.PublishToWorkspace(slackTeam.TeamId!, slackTeam.FplBotSlackChannel!, formatted);
        }
        else
        {
            logger.LogInformation("All price changes were irrelevant, so not sending any notification");
        }
    }
}
