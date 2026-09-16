using Discord.Net.HttpClients;
using FplBot.Data.Discord;

namespace FplBot.Discord;

public record ProbeResult(bool Delivered, string? Reason, string? Problem);

public class ChannelDeliveryProbe(
    IDiscordClient discordClient,
    IGuildRepository repo,
    ILogger<ChannelDeliveryProbe> logger)
{
    public async Task<ProbeResult> Probe(string guildId, string channelId, string message)
    {
        try
        {
            await discordClient.ChannelMessagePost(channelId, message);
            await ClearFailures(guildId, channelId);
            return new ProbeResult(true, null, null);
        }
        catch (DiscordApiException e)
        {
            logger.LogWarning(e, "Probe failed for guild {GuildId} channel {ChannelId}", guildId, channelId);
            return new ProbeResult(false, e.Message, Describe(e));
        }
    }

    private static string Describe(DiscordApiException e) => e.ErrorCode switch
    {
        10003 => "Discord says it doesn't know this channel, which usually means @fplbot's role can't see it. Check that the role has access to this channel.",
        50001 => "@fplbot can't see this channel - its role is missing access.",
        50013 => "@fplbot does not have permissions to post in this channel.",
        _ => $"Discord rejected the post: {e.Message}"
    };

    private const string FixThenPing = "Fix that - and get back here to test with `/ping`.";

    public static string ProblemAndFix(ProbeResult result) => $"{result.Problem} {FixThenPing}";

    private async Task ClearFailures(string guildId, string channelId)
    {
        var subscription = await repo.GetChannelSubscription(guildId, channelId);
        if (subscription is null || subscription.FailureCount == 0)
        {
            return;
        }

        subscription.ClearDeliveryFailures();
        await repo.SaveChannelSubscription(guildId, subscription);
    }
}
