using FplBot.Data.Slack;
using FplBot.Formatting;
using FplBot.WebApi.Slack.Abstractions;
using FplBot.WebApi.Slack.Helpers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

internal class FplTransfersCommandHandler(
    ISlackWorkSpacePublisher workSpacePublisher,
    IGameweekHelper gameweekHelper,
    ITransfersByGameWeek transfersByGameweek,
    ISlackTeamRepository slackTeamRepo)
    : HandleAppMentionBase
{
    public override string[] Commands => new[] { "transfers" };

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
    {
        var gameweek = await gameweekHelper.ExtractGameweekOrFallbackToCurrent(message.Text, $"{CommandsFormatted} {{gw}}");


        var team = await slackTeamRepo.GetTeam(eventMetadata.Team_Id);
        var messageToSend = "You don't follow any league yet. Use the `@fplbot follow` command first.";
        if (team.FplbotLeagueId.HasValue)
        {
            try
            {
                messageToSend =
                    await transfersByGameweek.GetTransfersByGameweekTexts(gameweek ?? 1, team.FplbotLeagueId.Value);
            }
            catch (HttpRequestException e) when (e.Message.Contains("429"))
            {
                messageToSend = "It seems fetching transfers was a bit heavy for this league. Try again later. 🤷‍️";
            }
        }

        await workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, messageToSend);
        return new EventHandledResponse(messageToSend);
    }

    public override (string,string) GetHelpDescription() => ($"{CommandsFormatted} {{GW-number, or empty for current}}", "Displays each team's transfers");
}
