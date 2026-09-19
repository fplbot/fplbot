using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class StandingsCommandHandler(IGlobalSettingsClient globalSettingsClient, ISlackTeamRepository teamRepo)
    : IConsumer<ProcessStandingsCommand>
{
    public async Task Consume(ConsumeContext<ProcessStandingsCommand> context)
    {
        var command = context.Message;
        var installation = await teamRepo.GetInstallation(command.TeamId);
        var settings = await globalSettingsClient.GetGlobalSettings();
        var gameweek = settings!.Gameweeks.GetCurrentGameweek();
        var channel = installation.GetChannel(command.ChannelId);
        if (channel?.FollowedLeagueId is { } leagueId)
        {
            await context.Publish(new PublishStandingsToSlackWorkspace(installation.Id, command.ChannelId, (int)leagueId.Value, gameweek!.Id));
        }
    }
}
