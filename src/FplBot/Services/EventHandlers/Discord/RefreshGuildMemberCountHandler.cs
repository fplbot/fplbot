using Discord.Net.HttpClients;
using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

// A fetch failure just skips this guild for this sweep - it keeps its last-known (or no) count
// until the next successful sweep. Guild reachability/removal is handled entirely by
// DiscordChannelDeliveryFailedHandler's failure-count cleanup, not here.
public class RefreshGuildMemberCountHandler(
    IGuildMemberCountRepository guildMemberCountRepo,
    DiscordClient discordClient,
    ILogger<RefreshGuildMemberCountHandler> logger) : IConsumer<RefreshGuildMemberCount>
{
    public async Task Consume(ConsumeContext<RefreshGuildMemberCount> context)
    {
        var guildId = context.Message.GuildId;
        try
        {
            var guild = await discordClient.GuildGet(guildId);
            var isCommunity = guild.Features?.Contains("COMMUNITY") ?? false;
            await guildMemberCountRepo.SetApproximateMemberCount(guildId, guild.ApproximateMemberCount, isCommunity);
        }
        catch (Exception e)
        {
            logger.LogInformation("MemberCount: {GuildId} SKIPPED. Exception: '{ExceptionMessage}'", guildId, e);
        }
    }
}
