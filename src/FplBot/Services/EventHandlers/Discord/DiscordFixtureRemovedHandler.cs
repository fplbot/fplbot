using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordFixtureRemovedHandler(
    IGuildRepository guildRepo,
    ILogger<DiscordFixtureRemovedHandler> logger)
    : IConsumer<FixtureRemovedFromGameweek>
{
    public async Task Consume(ConsumeContext<FixtureRemovedFromGameweek> context)
    {
        var message = context.Message;
        logger.LogInformation("Fixture removed from gameweek {Message}", message);
        var installations = await guildRepo.GetAllInstallations();

        foreach (var installation in installations)
        {
            foreach (var channel in installation.GetSubscriptionsTo(FplEvent.FixtureRemovedFromGameweek))
            {
                var formattedMsg = new PublishRichToGuildChannel(installation.Id,
                    channel.ChannelId,
                    "❌ Fixture off!",
                    $"{message.RemovedFixture.Home.Name}-{message.RemovedFixture.Away.Name}" +
                    $" has been removed from gameweek {message.Gameweek}!");
                await context.Publish(formattedMsg);
            }
        }
    }
}
