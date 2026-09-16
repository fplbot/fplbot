using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordNearDeadlineHandler(IGuildRepository teamRepo, ILogger<DiscordNearDeadlineHandler> logger)
    :
        IConsumer<OneHourToDeadline>,
        IConsumer<TwentyFourHoursToDeadline>
{
    public async Task Consume(ConsumeContext<OneHourToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 60 minutes to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.Deadlines);
        var text = $"😱 Gameweek {message.GameweekNearingDeadline.Id} deadline in 60 minutes! @here";
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToGuildChannel(guildId, channelId, text));
        }
    }

    public async Task Consume(ConsumeContext<TwentyFourHoursToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 24 hours to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var subscribedChannels = await teamRepo.GetChannelsSubscribedTo(FplEvent.Deadlines);
        var text = $"⏳Gameweek {message.GameweekNearingDeadline.Id} deadline in 24 hours!";
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToGuildChannel(guildId, channelId, $"{text}"));
        }
    }
}
