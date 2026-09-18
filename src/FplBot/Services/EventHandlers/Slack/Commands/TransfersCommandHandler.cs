using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.WebApi.Slack.Helpers;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class TransfersCommandHandler(
    ISlackWorkSpacePublisher workSpacePublisher,
    IGameweekHelper gameweekHelper,
    ITransfersByGameWeek transfersByGameweek,
    ISlackTeamRepository slackTeamRepo)
    : IConsumer<ProcessTransfersCommand>
{
    public async Task Consume(ConsumeContext<ProcessTransfersCommand> context)
    {
        var command = context.Message;
        var gameweek = await gameweekHelper.ExtractGameweekOrFallbackToCurrent(command.Text, "transfers {gw}");

        var installation = await slackTeamRepo.GetInstallation(command.TeamId);
        var leagueId = installation.GetChannel(command.ChannelId)?.FollowedLeagueId?.Value;
        var messageToSend = "You don't follow any league yet. Use the `@fplbot follow` command first.";
        if (leagueId.HasValue)
        {
            try
            {
                messageToSend =
                    await transfersByGameweek.GetTransfersByGameweekTexts(gameweek ?? 1, (int)leagueId.Value);
            }
            catch (HttpRequestException e) when (e.Message.Contains("429"))
            {
                messageToSend = "It seems fetching transfers was a bit heavy for this league. Try again later. 🤷‍️";
            }
        }

        await workSpacePublisher.PublishToWorkspace(command.TeamId, command.ChannelId, messageToSend);
    }
}
