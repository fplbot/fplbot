using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class SlackFixtureRemovedHandler(
    ISlackTeamRepository teamRepo,
    ILogger<SlackFixtureRemovedHandler> logger)
    : IConsumer<FixtureRemovedFromGameweek>
{
    public async Task Consume(ConsumeContext<FixtureRemovedFromGameweek> context)
    {
        var message = context.Message;
        logger.LogInformation("Fixture removed from gameweek {Message}", message);

        var installations = await teamRepo.GetAllInstallations();
        foreach (var installation in installations)
        {
            foreach (var channel in installation.GetSubscriptionsTo(FplEvent.FixtureRemovedFromGameweek))
            {
                var fixture = $"{message.RemovedFixture.Home.Name}-{message.RemovedFixture.Away.Name}";
                var msg = $"❌ *Fixture off!*\n {fixture} has been removed from gameweek {message.Gameweek}!";
                await context.Publish(new PublishToSlack(installation.TeamId, channel.ChannelId, msg));
            }
        }
    }
}
