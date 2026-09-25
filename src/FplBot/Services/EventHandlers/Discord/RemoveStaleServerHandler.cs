using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class RemoveStaleServerHandler(ILogger<RemoveStaleServerHandler> logger) : IConsumer<PurgedLastSubscriptionForServer>
{
    public async Task Consume(ConsumeContext<PurgedLastSubscriptionForServer> context)
    {
        var guildId = context.Message.GuildId;
        logger.LogWarning("Guild {GuildId} has no remaining channel subscriptions after a delivery-failure purge, uninstalling", guildId);
        await context.Publish(new UninstallGuild(guildId, UninstallReason.AutoPurged));
    }
}
