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
        var subscribedChannels = await guildRepo.GetChannelsSubscribedTo(FplEvent.FixtureRemovedFromGameweek);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            var formattedMsg = new PublishRichToGuildChannel(guildId,
                channelId,
                "❌ Fixture off!",
                $"{message.RemovedFixture.Home.Name}-{message.RemovedFixture.Away.Name}" +
                $" has been removed from gameweek {message.Gameweek}!");
            await context.Publish(formattedMsg);
        }
    }
}
