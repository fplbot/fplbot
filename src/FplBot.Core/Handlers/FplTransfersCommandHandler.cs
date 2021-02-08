using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Core.Handlers
{
    internal class FplTransfersCommandHandler : HandleAppMentionBase
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

        public override string[] Commands => new[] { "transfers" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
        {
            var gameweek = await _gameweekHelper.ExtractGameweekOrFallbackToCurrent(message.Text, $"{CommandsFormatted} {{gw}}");

            
            var team = await _slackTeamRepo.GetTeam(eventMetadata.Team_Id);
            var messageToSend = await _transfersClient.GetTransfersByGameweekTexts(gameweek ?? 1, (int)team.FplbotLeagueId);
            

            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, messageToSend);
            return new EventHandledResponse(messageToSend);
        }

        public override (string,string) GetHelpDescription() => ($"{CommandsFormatted} {{GW-number, or empty for current}}", "Displays each team's transfers");
    }
}
