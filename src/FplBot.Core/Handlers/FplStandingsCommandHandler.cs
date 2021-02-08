using System;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using FplBot.Core.Abstractions;
using FplBot.Core.Extensions;
using FplBot.Core.Helpers;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Core.Handlers
{
    internal class FplStandingsCommandHandler : HandleAppMentionBase
    {
        private readonly ISlackWorkSpacePublisher _workspacePublisher;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILeagueClient _leagueClient;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILogger<FplStandingsCommandHandler> _logger;

        public FplStandingsCommandHandler(ISlackWorkSpacePublisher workspacePublisher, IGameweekClient gameweekClient, ILeagueClient leagueClient, ISlackTeamRepository teamRepo, ILogger<FplStandingsCommandHandler> logger)
        {
            _workspacePublisher = workspacePublisher;
            _gameweekClient = gameweekClient;
            _leagueClient = leagueClient;
            _teamRepo = teamRepo;
            _logger = logger;
        }

        public override string[] Commands => new[] { "standings" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
        {
            var standings = await GetStandings(eventMetadata.Team_Id);
            await _workspacePublisher.PublishToWorkspace(eventMetadata.Team_Id, appMentioned.Channel, standings);
            return new EventHandledResponse(standings);
        }

        private async Task<string> GetStandings(string teamId)
        {
            try
            {
                var setup = await _teamRepo.GetTeam(teamId);
                var league = await _leagueClient.GetClassicLeague((int)setup.FplbotLeagueId);
                var gameweeks = await _gameweekClient.GetGameweeks();
                var standings = Formatter.GetStandings(league, gameweeks.GetCurrentGameweek());
                return standings;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message,e);
                return $"Oops, could not fetch standings.";
            }
        }
        
        public override (string,string) GetHelpDescription() => (CommandsFormatted, "Get current league standings");
    }
}