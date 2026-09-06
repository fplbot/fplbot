using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class BroadcastHandler(IGuildRepository repo, ILogger<BroadcastHandler> logger) : IConsumer<BroadcastToDiscord>
{
    public async Task Consume(ConsumeContext<BroadcastToDiscord> context)
    {
        var message = context.Message;
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
                await SendToGuild(message, context, guild, guildfilter);
            }
            else
            {
                if (guild.GuildId == "893932860162064414")
                {
                    await SendToGuild(message, context, guild, guildfilter);
                }
            }

            i++;
        }
    }

    private async Task SendToGuild(BroadcastToDiscord message, ConsumeContext context, GuildFplSubscription guild,
        Func<GuildFplSubscription, bool> filter)
    {
        if (filter(guild))
        {
            logger.LogInformation("Sending message to {GuildId} {ChannelId}", guild.GuildId, guild.ChannelId);
            await context.Publish(new PublishToGuildChannel(guild.GuildId, guild.ChannelId, message.Message));
        }
        else
        {
            logger.LogInformation("Did not pass filter. Not sending message to {GuildId} {ChannelId}", guild.GuildId, guild.ChannelId);
        }
    }
}
