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

        var installations = await repo.GetAllInstallations();

        var devOnly = message.Filter is
            ChannelFilter.AllChannelsDevServer or
            ChannelFilter.OnlyChannelsFollowingALeagueDevServer;

        foreach (var installation in installations)
        {
            if (!devOnly || installation.Id == "1546966580007542937")
            {
                await SendToInstallation(message, context, installation);
            }
        }
    }

    private async Task SendToInstallation(BroadcastToDiscord message, ConsumeContext context, Installation installation)
    {
        foreach (var channel in installation.ChannelSubscriptions)
        {
            if (PassesBroadcastFilter(channel))
            {
                logger.LogInformation("Sending message to {GuildId} {ChannelId}", installation.Id, channel.ChannelId);
                await context.Publish(new PublishToGuildChannel(installation.Id, channel.ChannelId, message.Message));
            }
            else
            {
                logger.LogInformation("Did not pass filter. Not sending message to {GuildId} {ChannelId}", installation.Id, channel.ChannelId);
            }
        }
    }

    private static bool PassesBroadcastFilter(ChannelSubscription channel) =>
        channel.IsSubscribedTo(FplEvent.Captains) ||
        channel.IsSubscribedTo(FplEvent.Transfers) ||
        channel.IsSubscribedTo(FplEvent.Standings);
}
