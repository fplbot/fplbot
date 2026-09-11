using FplBot.Discord;

namespace Discord.Net.HttpClients;

public class DevLoggingDiscordClient(DiscordClient inner, IHostEnvironment env, ILogger<DevLoggingDiscordClient> logger)
    : IDiscordClient
{
    public async Task ChannelMessagePost(string channelId, string text)
    {
        if (!env.IsDevelopment()) { await inner.ChannelMessagePost(channelId, text); return; }
        logger.LogInformation("[DEV] Discord → channel:{ChannelId}\n{Text}", channelId, text);
    }

    public async Task ChannelMessagePost(string channelId, DiscordClient.RichEmbed embed)
    {
        if (!env.IsDevelopment()) { await inner.ChannelMessagePost(channelId, embed); return; }
        logger.LogInformation("[DEV] Discord → channel:{ChannelId} | {Title}\n{Description}", channelId, embed.Title, embed.Description);
    }

    public async Task ApplicationsCommandPost(string name, string description, string? guildId, params ApplicationCommandOptions[] options)
    {
        if (!env.IsDevelopment()) { await inner.ApplicationsCommandPost(name, description, guildId, options); return; }
        logger.LogInformation("[DEV] Discord applications.commands.post → guild:{GuildId} /{Name} (not calling real API)", guildId, name);
    }

    public async Task ApplicationsCommandForGuildDelete(string guildId, string commandId)
    {
        if (!env.IsDevelopment()) { await inner.ApplicationsCommandForGuildDelete(guildId, commandId); return; }
        logger.LogInformation("[DEV] Discord applications.commands.delete → guild:{GuildId}/{CommandId} (not calling real API)", guildId, commandId);
    }

    // Fake "installed" commands mirror DiscordSlashCommandsEnsurer's own defined set, so the
    // admin UI's "currently installed" table shows something sensible in dev without ever
    // reaching Discord's API — same reasoning as DevLoggingSlackClient's seed-matching fakes.
    public Task<IEnumerable<DiscordClient.ApplicationsCommand>> ApplicationsCommandForGuildGet(string guildId)
    {
        if (!env.IsDevelopment()) return inner.ApplicationsCommandForGuildGet(guildId);
        logger.LogInformation("[DEV] Discord applications.commands.get → guild:{GuildId} (fake)", guildId);
        var fake = DiscordSlashCommandsEnsurer.GetDefinedCommands()
            .Select((c, i) => new DiscordClient.ApplicationsCommand((i + 1).ToString(), c.Name, c.Description));
        return Task.FromResult(fake);
    }
}
