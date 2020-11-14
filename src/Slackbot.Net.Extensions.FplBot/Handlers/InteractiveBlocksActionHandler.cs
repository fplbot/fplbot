using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Interactive.BlockActions;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class InteractiveBlocksActionHandler : IHandleInteractiveBlockActions
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILeagueClient _leagueClient;
        
        public InteractiveBlocksActionHandler(ISlackTeamRepository teamRepo, ILeagueClient leagueClient)
        {
            _teamRepo = teamRepo;
            _leagueClient = leagueClient;
        }
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

            await _leagueClient.GetClassicLeague(newLeagueID);
            await _teamRepo.UpdateLeagueId(blockActionEvent.Team.Id, newLeagueID);
            


            return new EventHandledResponse("League ID updated");
        }
    }
}