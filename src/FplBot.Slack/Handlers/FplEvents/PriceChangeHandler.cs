using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Slack.Handlers.FplEvents;

public class PriceChangeHandler : IHandleMessages<PlayersPriceChanged>, IHandleMessages<PublishPriceChangesToSlackWorkspace>
{
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly ISlackTeamRepository _slackTeamRepo;
    private readonly ILogger<PriceChangeHandler> _logger;

    public PriceChangeHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ILogger<PriceChangeHandler> logger)
    {
        _publisher = publisher;
        _slackTeamRepo = slackTeamRepo;
        _logger = logger;
    }

    public async Task Handle(PlayersPriceChanged notification, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Handling {notification.PlayersWithPriceChanges.Count()} price updates");
        var slackTeams = await _slackTeamRepo.GetAllTeams();
        foreach (var slackTeam in slackTeams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.PriceChanges))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishPriceChangesToSlackWorkspace(slackTeam.TeamId, notification.PlayersWithPriceChanges.ToList()), options);
            }
        }
    }

    public async Task Handle(PublishPriceChangesToSlackWorkspace message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Publish price changes to {message.WorkspaceId}");
        var filtered = message.PlayersWithPriceChanges.Where(c => c.OwnershipPercentage > 7);
        if (filtered.Any())
        {
            var slackTeam = await _slackTeamRepo.GetTeam(message.WorkspaceId);
            var formatted = Formatter.FormatPriceChanged(filtered);
            await _publisher.PublishToWorkspace(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted);
        }
        else
        {
            _logger.LogInformation("All price changes were irrelevant, so not sending any notification");
        }
    }
}
