using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;

namespace FplBot.Discord.Handlers.SlashCommands;

public class PingSlashCommandHandler(ChannelDeliveryProbe probe) : ISlashCommandHandler
{
    private const int Ephemeral = 64;

    public string CommandName => "ping";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var result = await probe.Probe(context.GuildId, context.ChannelId, "🏓 pong");

        return result.Delivered
            ? Respond("✅ PING", "Posted a message to this channel - your setup works.")
            : Respond("❌ PING", $"{result.Problem} Fix that, then run `/ping` again.");
    }

    private static ChannelMessageWithSourceEmbedResponse Respond(string title, string content) =>
        new() { Embeds = [new RichEmbed(title, content)], Flags = Ephemeral };
}
