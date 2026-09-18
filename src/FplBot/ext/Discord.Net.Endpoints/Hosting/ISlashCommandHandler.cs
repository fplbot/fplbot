using Discord.Net.Endpoints.Middleware;

namespace Discord.Net.Endpoints.Hosting;

public interface ISlashCommandHandler
{
    string CommandName { get; }

    string? SubCommandName => null;

    Task<SlashCommandResponse> Handle(SlashCommandContext context);
}
