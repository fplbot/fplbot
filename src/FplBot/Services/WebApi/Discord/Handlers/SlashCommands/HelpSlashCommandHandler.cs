using Discord.Net.Endpoints.Hosting;
using Discord.Net.Endpoints.Middleware;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.Discord.Handlers.SlashCommands;

public class HelpSlashCommandHandler(IPublishEndpoint publishEndpoint) : ISlashCommandHandler
{
    public string CommandName => "help";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        if (ChannelPermissions.Problem(context.AppPermissions) is { } problem)
        {
            return new ChannelMessageWithSourceEmbedResponse { Embeds = [new("ℹ️ HELP", $"⚠️ {problem}")] };
        }

        await publishEndpoint.Publish(new ProcessDiscordHelpCommand(context.GuildId, context.ChannelId, context.InteractionToken));
        return new DeferredResponse();
    }
}
