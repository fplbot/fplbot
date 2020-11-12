using Fpl.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Models;

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
                if (!team.Subscriptions.ContainsSubscriptionFor(EventSubscription.Standings))
                {
                    _logger.LogInformation("Team {team} hasn't subscribed for gw standings, so bypassing it", team.TeamId);
                    return;
                }

                try
                {
                    var league = await _leagueClient.GetClassicLeague((int)team.FplbotLeagueId);
                    
                    var intro = GetIntroText(gw, league);
                    var standings = Formatter.GetStandings(league, gw);
                    var topThree = Formatter.GetTopThreeGameweekEntries(league, gw);
                    var worst = Formatter.GetWorstGameweekEntry(league, gw);

                    await _publisher.PublishToWorkspace(team.TeamId, team.FplBotSlackChannel, intro, standings, topThree, worst);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        private static string GetIntroText(Gameweek gw, ClassicLeague league)
        {
            var introText = $"Gameweek {gw.Name} is finished.";
            var globalAverage = gw.AverageScore;
            var leagueAverage = Math.Floor(league.Standings.Entries.Average(entry => entry.EventTotal));
            var diff = Math.Abs(globalAverage - leagueAverage);
            var nuance = diff <= 5 ? "slightly " : "";

            if (globalAverage < 40)
            {
                introText += $" It was probably a disappointing one, with a global average of *{gw.AverageScore}* points.";
            }
            else if (globalAverage > 80)
            {
                introText += $" Must've been pretty intense, with a global average of *{globalAverage}* points.";
            }
            else
            {
                introText += $" The global average was {globalAverage} points.";
            }

            if (leagueAverage > globalAverage)
            {
                introText += $" Your league did {nuance}better than this, though - with *{leagueAverage}* points average.";
            }
            else
            {
                introText += $" I'm afraid your league did {nuance}worse than this, with your *{leagueAverage}* points average.";
            }

            return introText;
        }
    }
}