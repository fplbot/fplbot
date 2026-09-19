using System.Diagnostics;

namespace Discord.Net.Endpoints;

public static class DiscordDiagnostics
{
    public const string ActivitySourceName = "Discord.Net.Endpoints";

    public const string GuildIdTag = "discord.guild_id";
    public const string ChannelIdTag = "discord.channel_id";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
