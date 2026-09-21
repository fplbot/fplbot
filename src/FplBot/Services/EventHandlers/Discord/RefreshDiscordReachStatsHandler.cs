using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.EventHandlers;
using MassTransit;
using Microsoft.Extensions.Options;

namespace FplBot.EventHandlers.Discord;

// Dispatch only, per CLAUDE.md's fan-out pattern: the per-guild Discord API call happens in
// RefreshGuildMemberCountHandler, one message per guild. Each guild's command is scheduled with an
// increasing delay via MassTransit's message scheduler, so the resulting GuildGet calls spread out
// over the sweep instead of bursting all of them onto Discord's API at once - without blocking this
// consumer on Task.Delay while it does so.
public class RefreshDiscordReachStatsHandler(IGuildRepository guildRepo, IOptions<ReachStatsSweepOptions> options) : IConsumer<RefreshDiscordReachStats>
{
    public async Task Consume(ConsumeContext<RefreshDiscordReachStats> context)
    {
        var delayBetweenGuilds = options.Value.DelayBetweenDiscordGuilds;
        var index = 0;
        foreach (var guild in await guildRepo.GetAllInstallations())
        {
            await context.SchedulePublish(delayBetweenGuilds * index, new RefreshGuildMemberCount(guild.ExternalId));
            index++;
        }
    }
}
