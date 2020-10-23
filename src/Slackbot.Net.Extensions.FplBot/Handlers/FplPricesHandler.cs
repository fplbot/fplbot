using System.Linq;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplPricesHandler : IHandleAppMentions
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

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
        {
            var allPlayers = await _playerClient.GetAllPlayers();
            var teams = await _teamsClient.GetAllTeams();
            var priceChangedPlayers = allPlayers.Where(p => p.CostChangeEvent != 0);
            var messageToSend = Formatter.FormatPriceChanged(priceChangedPlayers, teams);
            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, messageToSend);
            return new EventHandledResponse(messageToSend);
        }

        public bool ShouldHandle(AppMentionEvent @event) => @event.Text.Contains("pricechanges");

        public (string,string) GetHelpDescription() => ("pricechanges", "Displays players with recent price change");
    }
}
