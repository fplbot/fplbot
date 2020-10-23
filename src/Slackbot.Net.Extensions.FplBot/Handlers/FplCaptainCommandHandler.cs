using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System.Threading.Tasks;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplCaptainCommandHandler : IHandleAppMentions
    {
        private readonly ICaptainsByGameWeek _captainsByGameWeek;
        private readonly IGameweekHelper _gameweekHelper;
        private readonly ISlackTeamRepository _slackTeamsRepo;
        private readonly ISlackWorkSpacePublisher _workspacePublisher;

        public FplCaptainCommandHandler(
            ICaptainsByGameWeek captainsByGameWeek,  
            IGameweekHelper gameweekHelper,
            ISlackTeamRepository slackTeamsRepo,
            ISlackWorkSpacePublisher workspacePublisher
           )
        {
            _captainsByGameWeek = captainsByGameWeek;
            _gameweekHelper = gameweekHelper;
            _slackTeamsRepo = slackTeamsRepo;
            _workspacePublisher = workspacePublisher;
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent incomingMessage)
        {
            var isChartRequest = incomingMessage.Text.Contains("chart");

            var gwPattern = "captains {gw}";
            if (isChartRequest)
            {
                gwPattern = "captains chart {gw}|captains {gw} chart";
            }
            var gameWeek = await _gameweekHelper.ExtractGameweekOrFallbackToCurrent(new MessageHelper(), incomingMessage.Text, gwPattern);

            if (!gameWeek.HasValue)
            {
                 await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, incomingMessage.Channel, "Invalid gameweek :grimacing:");
                 return new EventHandledResponse("Invalid gameweek");
            }

            var setup = await _slackTeamsRepo.GetTeam(eventMetadata.Team_Id);

            var outgoingMessage = isChartRequest ? 
                await _captainsByGameWeek.GetCaptainsChartByGameWeek(gameWeek.Value, (int)setup.FplbotLeagueId) : 
                await _captainsByGameWeek.GetCaptainsByGameWeek(gameWeek.Value, (int)setup.FplbotLeagueId);

            await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, incomingMessage.Channel, outgoingMessage);
       
            return new EventHandledResponse(outgoingMessage);
        }

        public bool ShouldHandle(AppMentionEvent @event) => @event.Text.Contains("captains");

        public (string,string) GetHelpDescription() => ("captains [chart] {GW/''}", "Display captain picks in the league. Add \"chart\" to visualize it in a chart.");
    }
}
