using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Interactive.BlockActions;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class InteractiveBlocksActionHandler(ISlackTeamRepository teamRepo, ILeagueClient leagueClient)
    : IHandleInteractiveBlockActions
{
    public async Task<EventHandledResponse> Handle(BlockActionInteraction blockActionEvent)
    {
        var actionsBlock = blockActionEvent.Actions.FirstOrDefault(x => x.action_id.Equals("fpl_league_id_action"));

        if (actionsBlock == null)
        {
            return new EventHandledResponse("IGNORE. THIS IS NOT FOR ME");
        }

        var leagueId = actionsBlock.value;

        if (!int.TryParse(leagueId, out var newLeagueID))
        {
            return new EventHandledResponse("VALIDATION_ERRORS");
        }

        try
        {
            await leagueClient.GetClassicLeague(newLeagueID);
        }
        catch (Exception)
        {
            return new EventHandledResponse("VALIDATION_ERRORS");
        }

        await teamRepo.UpdateLeagueId(blockActionEvent.Team.Id, newLeagueID);



        return new EventHandledResponse("League ID updated");
    }
}
