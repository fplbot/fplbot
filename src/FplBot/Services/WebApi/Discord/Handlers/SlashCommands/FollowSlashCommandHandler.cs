using Discord.Net.Endpoints.Hosting;
using FplBot.EventHandlers.Discord;
using Discord.Net.Endpoints.Middleware;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.Discord.Handlers.SlashCommands;

public class FollowSlashCommandHandler(IPublishEndpoint publishEndpoint) : ISlashCommandHandler
{
    public string CommandName => "follow";

    public async Task<SlashCommandResponse> Handle(SlashCommandContext context)
    {
        var leagueId = int.Parse(context.CommandInput!.Value);
        var problem = ChannelPermissions.Problem(context.AppPermissions);

        await publishEndpoint.Publish(new ProcessFollowCommand(
            context.GuildId,
            context.ChannelId,
            problem is null ? context.InteractionToken : string.Empty,
            leagueId,
            context.AppPermissions));

        return problem is null
            ? new DeferredResponse()
            : new ChannelMessageWithSourceComponentsResponse(
                DiscordCards.HeadingCard("⚠️ Saved, but I can't post here yet", problem));
    }
}
