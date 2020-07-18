using System.Linq;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplPricesHandler : IHandleEvent
    {
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;
        private readonly IPlayerClient _playerClient;

        public FplPricesHandler(ISlackWorkSpacePublisher workSpacePublisher, IPlayerClient playerClient)
        {
            _workSpacePublisher = workSpacePublisher;
            _playerClient = playerClient;
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var message = slackEvent as AppMentionEvent;
            var allPlayers = await _playerClient.GetAllPlayers();
            var priceChangedPlayers = allPlayers.Where(p => p.CostChangeEvent != 0);
            var messageToSend = Formatter.FormatPriceChanged(priceChangedPlayers);
            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, messageToSend);
            return new EventHandledResponse(messageToSend);
        }

        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("pricechanges");

        public (string,string) GetHelpDescription() => ("pricechanges", "Displays players with recent price change");
    }
}
