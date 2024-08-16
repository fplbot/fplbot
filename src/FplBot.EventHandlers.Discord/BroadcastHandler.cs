using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Discord;

public class BroadcastHandler(IGuildRepository repo, ILogger<BroadcastHandler> logger) : IHandleMessages<BroadcastToDiscord>
{
    public async Task Handle(BroadcastToDiscord message, IMessageHandlerContext context)
    {
        logger.LogInformation("BROADCASTING {Message} TO DISCORD USING filter {ChannelFilter}", message.Message, message.Filter);

        if (message.Filter == ChannelFilter.NotSet)
        {
            logger.LogWarning("NOT BROADCASTING THE MESSAGE. Filter was {ChannelFilter}", message.Filter);
            return;
        }

        var allGuilds = await repo.GetAllGuildSubscriptions();

        var devOnly = message.Filter is
            ChannelFilter.AllChannelsDevServer or
            ChannelFilter.OnlyChannelsFollowingALeagueDevServer;

        Func<GuildFplSubscription, bool> guildfilter =
            someGuild => someGuild.Subscriptions.Any(c => c is
                                EventSubscription.Captains or
                                EventSubscription.Transfers or
                                EventSubscription.Standings or
                                EventSubscription.All);

        var i = 0;
        foreach (var guild in allGuilds)
        {
            if (!devOnly)
            {
                await SendToGuild(message, context, guild, guildfilter, 0);
            }
            else
            {
                if (guild.GuildId == "893932860162064414")
                {
                    var millisecondDelay = i*750;
                    await SendToGuild(message, context, guild, guildfilter, millisecondDelay);
                }
            }

            i++;
        }
    }

    private async Task SendToGuild(BroadcastToDiscord message, IMessageHandlerContext context, GuildFplSubscription guild,
        Func<GuildFplSubscription, bool> filter, int millisecondDelay)
    {
        if (filter(guild))
        {
            logger.LogInformation("Sending message to {GuildId} {ChannelId}", guild.GuildId, guild.ChannelId);
            var options = new SendOptions();
            options.RequireImmediateDispatch();
            options.RouteToThisEndpoint();
            options.DelayDeliveryWith(TimeSpan.FromMilliseconds(millisecondDelay));
            await context.Send(new PublishToGuildChannel(guild.GuildId, guild.ChannelId, message.Message), options);
        }
        else
        {
            logger.LogInformation("Did not pass filter. Not sending message to {GuildId} {ChannelId}", guild.GuildId, guild.ChannelId);
        }
    }
}
