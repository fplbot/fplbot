using Discord.Net.Endpoints.Hosting;
using FplBot.EventHandlers.Discord;
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
            return new ChannelMessageWithSourceComponentsResponse(DiscordCards.HeadingCard("ℹ️ HELP", $"⚠️ {problem}"));
        }

        await publishEndpoint.Publish(new ProcessDiscordHelpCommand(context.GuildId, context.ChannelId, context.InteractionToken));
        return new DeferredResponse();
    }
}
