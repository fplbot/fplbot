using System.Linq;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Data.Abstractions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Core.Handlers
{
    internal class FplCaptainCommandHandler : HandleAppMentionBase
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

        public override string[] Commands => new[] { "captains" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent incomingMessage)
        {
            var isChartRequest = incomingMessage.Text.Contains("chart");

            var gwPattern = $"{Commands.First()} {{gw}}";
            if (isChartRequest)
            {
                gwPattern = $"{Commands.First()} chart {{gw}}|{Commands.First()} {{gw}} chart";
            }
            var gameWeek = await _gameweekHelper.ExtractGameweekOrFallbackToCurrent(incomingMessage.Text, gwPattern);

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

        public override (string, string) GetHelpDescription() => ($"{CommandsFormatted} [chart] {{GW-number, or empty for current}}", "Display captain picks in the league. Add \"chart\" to visualize it in a chart.");
    }
}
