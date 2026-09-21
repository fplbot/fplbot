using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

// Dispatch only, per CLAUDE.md's fan-out pattern: the per-guild Discord API call happens in
// RefreshGuildMemberCountHandler, one message per guild. A small delay between publishes spreads
// the resulting GuildGet calls out over the sweep instead of bursting all of them onto Discord's
// API at once.
public class RefreshDiscordReachStatsHandler(IGuildRepository guildRepo) : IConsumer<RefreshDiscordReachStats>
{
    private static readonly TimeSpan DelayBetweenGuilds = TimeSpan.FromSeconds(1);

    public async Task Consume(ConsumeContext<RefreshDiscordReachStats> context)
    {
        foreach (var guild in await guildRepo.GetAllInstallations())
        {
            await context.Publish(new RefreshGuildMemberCount(guild.ExternalId));
            await Task.Delay(DelayBetweenGuilds, context.CancellationToken);
        }
    }
}
