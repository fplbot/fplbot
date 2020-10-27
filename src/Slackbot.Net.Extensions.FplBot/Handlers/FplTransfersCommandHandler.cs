using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplTransfersCommandHandler : IHandleAppMentions
    {
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;
        private readonly IGameweekHelper _gameweekHelper;
        private readonly ITransfersByGameWeek _transfersClient;
        private readonly ISlackTeamRepository _slackTeamRepo;

        public FplTransfersCommandHandler(ISlackWorkSpacePublisher workSpacePublisher, IGameweekHelper gameweekHelper, ITransfersByGameWeek transfersByGameweek, ISlackTeamRepository slackTeamRepo)
        {
            _workSpacePublisher = workSpacePublisher;
            _gameweekHelper = gameweekHelper;
            _transfersClient = transfersByGameweek;
            _slackTeamRepo = slackTeamRepo;
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
        {
            var gameweek = await _gameweekHelper.ExtractGameweekOrFallbackToCurrent(message.Text, "transfers {gw}");

            
            var team = await _slackTeamRepo.GetTeam(eventMetadata.Team_Id);
            var messageToSend = await _transfersClient.GetTransfersByGameweekTexts(gameweek ?? 1, (int)team.FplbotLeagueId);
            

            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, messageToSend);
            return new EventHandledResponse(messageToSend);
        }

        public bool ShouldHandle(AppMentionEvent @event) => @event.Text.Contains("transfers");

        public (string,string) GetHelpDescription() => ("transfers {GW-number, or empty for current}", "Displays each team's transfers");
    }
}
