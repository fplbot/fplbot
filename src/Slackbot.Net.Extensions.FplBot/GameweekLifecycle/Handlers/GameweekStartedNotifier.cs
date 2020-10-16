using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.RecurringActions;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekStartedNotifier : IHandleGameweekStarted
    {
        private readonly ICaptainsByGameWeek _captainsByGameweek;
        private readonly ITransfersByGameWeek _transfersByGameweek;
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILogger<GameweekStartedNotifier> _logger;

        public GameweekStartedNotifier(ICaptainsByGameWeek captainsByGameweek,
            ITransfersByGameWeek transfersByGameweek,
            ISlackWorkSpacePublisher publisher,
            ISlackTeamRepository teamsRepo,
            ILogger<GameweekStartedNotifier> logger)
        {
            _captainsByGameweek = captainsByGameweek;
            _transfersByGameweek = transfersByGameweek;
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _logger = logger;
        }

        public async Task HandleGameweekStarted(int newGameweek)
        {
            await _publisher.PublishToAllWorkspaceChannels($"Gameweek {newGameweek}!");
            var teams = await _teamRepo.GetAllTeams();

            foreach (var team in teams)
            {
                try
                {
                    var captains = await _captainsByGameweek.GetCaptainsByGameWeek(newGameweek, (int) team.FplbotLeagueId);
                    var captainsChart = await _captainsByGameweek.GetCaptainsChartByGameWeek(newGameweek, (int)team.FplbotLeagueId);
                    var transfers = await _transfersByGameweek.GetTransfersByGameweekTexts(newGameweek, (int) team.FplbotLeagueId);
                    await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, captains, captainsChart, transfers);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }
    }
}