using FplBot.Data.Slack;
using FplBot.Formatting.Helpers;
using FplBot.WebApi.Slack.Abstractions;
using FplBot.WebApi.Slack.Helpers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

internal class FplCaptainCommandHandler(
    ICaptainsByGameWeek captainsByGameWeek,
    IGameweekHelper gameweekHelper,
    ISlackTeamRepository slackTeamsRepo,
    ISlackWorkSpacePublisher workspacePublisher)
    : HandleAppMentionBase
{
    public override string[] Commands => new[] { "captains" };

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent incomingMessage)
    {
        var isChartRequest = incomingMessage.Text.Contains("chart");

        var gwPattern = $"{Commands.First()} {{gw}}";
        if (isChartRequest)
        {
            gwPattern = $"{Commands.First()} chart {{gw}}|{Commands.First()} {{gw}} chart";
        }
        var gameWeek = await gameweekHelper.ExtractGameweekOrFallbackToCurrent(incomingMessage.Text, gwPattern);

        if (!gameWeek.HasValue)
        {
            await workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, incomingMessage.Channel, "Invalid gameweek :grimacing:");
            return new EventHandledResponse("Invalid gameweek");
        }

        var setup = await slackTeamsRepo.GetTeam(eventMetadata.Team_Id);

        string outgoingMessage;
        if (setup.FplbotLeagueId.HasValue)
        {
            var captainPicks = await captainsByGameWeek.GetEntryCaptainPicks(gameWeek.Value, setup.FplbotLeagueId.Value);
            outgoingMessage = isChartRequest
                ? captainsByGameWeek.GetCaptainsChartByGameWeek(gameWeek.Value, captainPicks)
                : captainsByGameWeek.GetCaptainsByGameWeek(gameWeek.Value, captainPicks);
        }
        else
        {
            outgoingMessage = "No league. Follow a league first via `@fplbot follow`";
        }


        await workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, incomingMessage.Channel, outgoingMessage);

        return new EventHandledResponse(outgoingMessage);
    }

    public override (string, string) GetHelpDescription() => ($"{CommandsFormatted} [chart] {{GW-number, or empty for current}}", "Display captain picks in the league. Add \"chart\" to visualize it in a chart.");
}
