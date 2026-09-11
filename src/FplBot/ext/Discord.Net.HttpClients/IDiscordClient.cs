namespace Discord.Net.HttpClients;

public interface IDiscordClient
{
    Task ChannelMessagePost(string channelId, string text);
    Task ChannelMessagePost(string channelId, DiscordClient.RichEmbed embed);
    Task ApplicationsCommandPost(string name, string description, string? guildId, params ApplicationCommandOptions[] options);
    Task ApplicationsCommandForGuildDelete(string guildId, string commandId);
    Task<IEnumerable<DiscordClient.ApplicationsCommand>> ApplicationsCommandForGuildGet(string guildId);
}
