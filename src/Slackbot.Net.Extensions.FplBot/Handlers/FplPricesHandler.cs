using Fpl.Client.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Extensions.FplBot.Extensions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplPricesHandler : HandleAppMentionBase
    {
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;

        public FplPricesHandler(ISlackWorkSpacePublisher workSpacePublisher, IPlayerClient playerClient, ITeamsClient teamsClient)
        {
            _workSpacePublisher = workSpacePublisher;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
        }

        public override string[] Commands => new[] { "pricechanges" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
        {
            var allPlayers = await _playerClient.GetAllPlayers();
            var teams = await _teamsClient.GetAllTeams();
            var priceChangedPlayers = allPlayers.Where(p => p.CostChangeEvent != 0 && p.IsRelevant());
            if (priceChangedPlayers.Any())
            {
                var messageToSend = Formatter.FormatPriceChanged(priceChangedPlayers, teams);
                await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, messageToSend);    
            }
            else
            {
                await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, "No relevant price changes yet"); 
            }
            
            return new EventHandledResponse("Ok");
        }

        public override (string,string) GetHelpDescription() => (CommandsFormatted, "Displays players with recent price change");
    }
}
