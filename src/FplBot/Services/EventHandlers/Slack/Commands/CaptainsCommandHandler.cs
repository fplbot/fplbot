using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.WebApi.Slack.Helpers;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class CaptainsCommandHandler(
    ICaptainsByGameWeek captainsByGameWeek,
    IGameweekHelper gameweekHelper,
    ISlackTeamRepository slackTeamsRepo,
    ISlackWorkSpacePublisher workspacePublisher)
    : IConsumer<ProcessCaptainsCommand>
{
    public async Task Consume(ConsumeContext<ProcessCaptainsCommand> context)
    {
        var command = context.Message;
        var isChartRequest = command.Text.Contains("chart");

        var gwPattern = "captains {gw}";
        if (isChartRequest)
        {
            gwPattern = "captains chart {gw}|captains {gw} chart";
        }
        var gameWeek = await gameweekHelper.ExtractGameweekOrFallbackToCurrent(command.Text, gwPattern);

        if (!gameWeek.HasValue)
        {
            await workspacePublisher.PublishToWorkspace(command.TeamId, command.Channel, "Invalid gameweek :grimacing:");
            return;
        }

        var installation = await slackTeamsRepo.GetInstallation(command.TeamId);
        var leagueId = installation.GetChannel(command.Channel)?.FollowedLeagueId?.Value;

        string outgoingMessage;
        if (leagueId.HasValue)
        {
            var captainPicks = await captainsByGameWeek.GetEntryCaptainPicks(gameWeek.Value, (int)leagueId.Value);
            outgoingMessage = isChartRequest
                ? captainsByGameWeek.GetCaptainsChartByGameWeek(gameWeek.Value, captainPicks)
                : captainsByGameWeek.GetCaptainsByGameWeek(gameWeek.Value, captainPicks);
        }
        else
        {
            outgoingMessage = "No league. Follow a league first via `@fplbot follow`";
        }

        await workspacePublisher.PublishToWorkspace(command.TeamId, command.Channel, outgoingMessage);
    }
}
