using System.Diagnostics;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;

namespace FplBot.Discord.Handlers.SlashCommands;

public class TeamContextSlashCommandHandler(ISlashCommandHandler inner, ILogger<TeamContextSlashCommandHandler> logger)
    : ISlashCommandHandler
{
    public string CommandName => inner.CommandName;

    public string? SubCommandName => inner.SubCommandName;

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            [FplBotDiagnostics.TeamIdTag] = context.GuildId,
            [FplBotDiagnostics.ChannelIdTag] = context.ChannelId
        });
        Activity.Current?.SetTag(FplBotDiagnostics.TeamIdTag, context.GuildId);
        Activity.Current?.SetTag(FplBotDiagnostics.ChannelIdTag, context.ChannelId);
        logger.LogDebug("Handling slash command {CommandName}", inner.CommandName);
        return await inner.Handle(context);
    }
}
