using Discord.Net.Endpoints.Hosting;
using FplBot.EventHandlers.Discord;
using Discord.Net.Endpoints.Middleware;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.Discord.Handlers.SlashCommands;

public class StandingsSlashCommandHandler(IPublishEndpoint publishEndpoint) : ISlashCommandHandler
{
    public string CommandName => "standings";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        if (ChannelPermissions.Problem(context.AppPermissions) is { } problem)
        {
            return new ChannelMessageWithSourceComponentsResponse(DiscordCards.HeadingCard("📊 Standings", $"⚠️ {problem}"));
        }

        await publishEndpoint.Publish(new ProcessDiscordStandingsCommand(context.GuildId, context.ChannelId, context.InteractionToken));
        return new DeferredResponse();
    }
}
