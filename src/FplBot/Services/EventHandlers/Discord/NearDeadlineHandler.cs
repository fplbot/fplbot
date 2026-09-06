using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class NearDeadlineHandler(IGuildRepository teamRepo, ILogger<NearDeadlineHandler> logger)
    :
        IConsumer<OneHourToDeadline>,
        IConsumer<TwentyFourHoursToDeadline>
{
    public async Task Consume(ConsumeContext<OneHourToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 60 minutes to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var allGuilds = await teamRepo.GetAllGuildSubscriptions();
        var text = $"😱 Gameweek {message.GameweekNearingDeadline.Id} deadline in 60 minutes! @here";
        foreach (var guild in allGuilds)
        {
            if (guild.Subscriptions.ContainsSubscriptionFor(EventSubscription.Deadlines))
            {
                await context.Publish(new PublishToGuildChannel(guild.GuildId, guild.ChannelId, text));
            }
        }
    }

    public async Task Consume(ConsumeContext<TwentyFourHoursToDeadline> context)
    {
        var message = context.Message;
        logger.LogInformation($"Notifying about 24 hours to (gw{message.GameweekNearingDeadline.Id}) deadline");
        var allGuilds = await teamRepo.GetAllGuildSubscriptions();
        var text = $"⏳Gameweek {message.GameweekNearingDeadline.Id} deadline in 24 hours!";
        foreach (var guild in allGuilds)
        {
            if (guild.Subscriptions.ContainsSubscriptionFor(EventSubscription.Deadlines))
            {
                await context.Publish(new PublishToGuildChannel(guild.GuildId, guild.ChannelId, $"{text}"));
            }
        }
    }
}
