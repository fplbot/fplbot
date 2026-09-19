using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.Discord;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord.Commands;

public class FollowCommandHandler(
    ILeagueClient leagueClient,
    IGuildRepository repo,
    ILogger<FollowCommandHandler> logger)
    : IConsumer<ProcessFollowCommand>
{
    public async Task Consume(ConsumeContext<ProcessFollowCommand> context)
    {
        var command = context.Message;
        try
        {
            var league = await leagueClient.GetClassicLeague(command.LeagueId, tolerate404: true);

            if (league == null)
            {
                await Respond(context, "⚠️ Error", $"Could not find a classic league of id '{command.LeagueId}'");
                return;
            }

            var installation = await repo.GetInstallation(command.TeamId);
            var isNewChannel = installation.GetChannel(command.ChannelId) is null;

            installation.Follow(command.ChannelId, new ClassicLeagueId(command.LeagueId));
            await repo.Save(installation);

            var leagueName = league.Properties?.Name;
            if (ChannelPermissions.Problem(command.AppPermissions) is { } problem)
            {
                await Respond(context, "⚠️ Saved, but I can't post here yet", $"Following '{leagueName}'! {problem}");
                return;
            }

            await Respond(context, "✅ Success", isNewChannel
                ? $"Now following the '{leagueName}' FPL league. (Auto-subbed to all events) "
                : $"Now following the '{leagueName}' FPL league. ");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed following league {LeagueId} for guild {GuildId}", command.LeagueId, command.TeamId);
            await Respond(context, "⚠️ Error", "Something went wrong on my end. Try again in a moment.");
        }
    }

    private static async Task Respond(ConsumeContext<ProcessFollowCommand> context, string title, string description)
    {
        if (context.Message.InteractionToken is { Length: > 0 } token)
        {
            await context.Publish(new RespondToDiscordInteraction(token, title, description));
        }
    }
}
