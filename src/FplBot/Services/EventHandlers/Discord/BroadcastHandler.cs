using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class BroadcastHandler(IGuildRepository repo, ILogger<BroadcastHandler> logger) : IConsumer<BroadcastToDiscord>
{
    public async Task Consume(ConsumeContext<BroadcastToDiscord> context)
    {
        var message = context.Message;
        logger.LogInformation("HANDLING BROADCAST OF {Message} TO DISCORD USING filter {ChannelFilter}", message.Message, message.Filter);

        if (message.Filter == ChannelFilter.NotSet)
        {
            logger.LogWarning("NOT BROADCASTING THE MESSAGE. Filter was {ChannelFilter}", message.Filter);
            return;
        }

        var devOnly = message.Filter is
            ChannelFilter.AllChannelsDevServer or
            ChannelFilter.OnlyChannelsFollowingALeagueDevServer;

        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.Captains, FplEvent.Transfers, FplEvent.Standings);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            if (!devOnly || guildId == "1546966580007542937")
            {
                logger.LogInformation("Sending message to {GuildId} {ChannelId}", guildId, channelId);
                await context.Publish(new PublishToGuildChannel(guildId, channelId, message.Message));
            }
        }
    }
}
