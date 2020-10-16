using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class GameweekEndedNotifier : IHandleGameweekEnded
    {
        private readonly ISlackWorkSpacePublisher _publisher;
        private readonly ILeagueClient _leagueClient;
        private readonly IGameweekClient _gameweekClient;
        private readonly ILogger<GameweekEndedNotifier> _logger;
        private readonly ISlackTeamRepository _teamRepo;

        public GameweekEndedNotifier(ISlackWorkSpacePublisher publisher, 
            ISlackTeamRepository teamsRepo,
            ILeagueClient leagueClient, 
            IGameweekClient gameweekClient, 
            ILogger<GameweekEndedNotifier> logger)
        {
            _publisher = publisher;
            _teamRepo = teamsRepo;
            _leagueClient = leagueClient;
            _gameweekClient = gameweekClient;
            _logger = logger;
        }

        public async Task HandleGameweekEndeded(int gameweek)
        {
            var gameweeks = await _gameweekClient.GetGameweeks();
            var gw = gameweeks.SingleOrDefault(g => g.Id == gameweek);
            if (gw == null)
            {
                _logger.LogError("Found no gameweek with id {id}", gameweek);
                return;
            }

            var teams = await _teamRepo.GetAllTeams();
            foreach (var team in teams)
            {
                try
                {
                    var intro = GetIntroText(gw);

                    var league = await _leagueClient.GetClassicLeague((int)team.FplbotLeagueId);

                    var standings = Formatter.GetStandings(league, gw);
                    var best = Formatter.GetBestPerformer(league, gw);
                    var worst = Formatter.GetWorstPerformer(league, gw);

                    await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, intro, standings, best, worst);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        private string GetIntroText(Gameweek gw)
        {
            if (gw.AverageScore < 40) return "Well, that was a disappointing gameweek. Here's a roundup :point_down:";
            if (gw.AverageScore > 80) return "Wow, that gameweek was intense! Here's how everybody is doing :point_down:";
            return "Gameweek's over :wave: Here's standings and stuff :point_down:";
        }
    }
}