using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Microsoft.VisualBasic.CompilerServices;
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
            var newLeagueID = int.Parse(blockActionEvent.Actions.First(x => x.action_id.Equals("fpl_league_id_action")).value);

            await _leagueClient.GetClassicLeague(newLeagueID);
            await _teamRepo.UpdateLeagueId(blockActionEvent.Team.Id, newLeagueID);
            


            return new EventHandledResponse("League ID updated");
        }
    }
}