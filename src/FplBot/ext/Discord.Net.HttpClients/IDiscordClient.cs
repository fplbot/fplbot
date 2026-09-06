namespace Discord.Net.HttpClients;

public interface IDiscordClient
{
    Task ChannelMessagePost(string channelId, string text);
    Task ChannelMessagePost(string channelId, DiscordClient.RichEmbed embed);
}
