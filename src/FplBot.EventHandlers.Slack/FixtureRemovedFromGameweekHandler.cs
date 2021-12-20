using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Slack;

public class FixtureRemovedFromGameweekHandler : IHandleMessages<FixtureRemovedFromGameweek>
{
    private readonly ISlackTeamRepository _teamRepo;
    private readonly ILogger<FixtureRemovedFromGameweekHandler> _logger;


    public FixtureRemovedFromGameweekHandler(ISlackTeamRepository teamRepo, ILogger<FixtureRemovedFromGameweekHandler> logger)
    {
        _teamRepo = teamRepo;
        _logger = logger;
    }

    public async Task Handle(FixtureRemovedFromGameweek message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Fixture removed from gameweek {Message}", message);

        var teams = await _teamRepo.GetAllTeams();
        foreach (var team in teams)
        {
            if (team.Subscriptions.ContainsSubscriptionFor(EventSubscription.FixtureAssists))
            {
                var fixture = $"{message.RemovedFixture.Home.Name}-{message.RemovedFixture.Away.Name}";
                var msg = $"❌ *Fixture off!*\n {fixture} has been removed from gameweek {message.Gameweek}!";
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishToSlack(team.TeamId, team.FplBotSlackChannel, msg), options);
            }
        }
    }
}
