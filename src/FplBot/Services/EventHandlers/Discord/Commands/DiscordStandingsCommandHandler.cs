using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Discord;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord.Commands;

public class DiscordStandingsCommandHandler(
    IGuildRepository repo,
    IGlobalSettingsClient globalSettingsClient,
    ILogger<DiscordStandingsCommandHandler> logger)
    : IConsumer<ProcessDiscordStandingsCommand>
{
    public async Task Consume(ConsumeContext<ProcessDiscordStandingsCommand> context)
    {
        var command = context.Message;
        try
        {
            var installation = await repo.FindInstallationByTeamId(command.TeamId);
            var channel = installation?.GetChannel(command.ChannelId);
            if (channel?.FollowedLeagueId is not { } leagueId)
            {
                await Respond(context, "⚠️ No league", "This channel isn't following an FPL league yet. Use `/follow` to pick one.");
                return;
            }

            var settings = await globalSettingsClient.GetGlobalSettings();
            if (settings?.Gameweeks.GetCurrentGameweek() is not { } gameweek)
            {
                await Respond(context, "⚠️ No gameweek", "There's no current gameweek to show standings for.");
                return;
            }

            await context.Publish(new PublishStandingsToDiscordGuild(command.TeamId, command.ChannelId, (int)leagueId.Value, gameweek.Id));
            await Respond(context, "📊 Standings", "Posting the standings in this channel.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed fetching standings for guild {GuildId}", command.TeamId);
            await Respond(context, "⚠️ Error", "Something went wrong on my end. Try again in a moment.");
        }
    }

    private static async Task Respond(ConsumeContext<ProcessDiscordStandingsCommand> context, string title, string description) =>
        await context.Publish(new RespondToDiscordInteraction(context.Message.InteractionToken, title, description));
}
