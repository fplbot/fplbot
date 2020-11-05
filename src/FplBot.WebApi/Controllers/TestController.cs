
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly ILeagueClient _leagueClient;
        private readonly IPlayerClient _playerclient;
        private readonly ITeamsClient _teamsClient;
        private readonly FixtureEventsHandler _eventsHandler;

        public TestController(ISlackTeamRepository teamRepo, ILeagueClient leagueClient, IPlayerClient playerclient, ITeamsClient teamsClient, FixtureEventsHandler eventsHandler, ILogger<FplController> logger)
        {
            _teamRepo = teamRepo;
            _leagueClient = leagueClient;
            _playerclient = playerclient;
            _teamsClient = teamsClient;
            _eventsHandler = eventsHandler;
        }

        [HttpGet("fixture-event")]
        public async Task<IActionResult> GetTestFixtureEvent()
        {
            var blankTeam = await _teamRepo.GetTeam("T0A9QSU83");
            blankTeam.FplbotLeagueId = 619747;
            blankTeam.Subscriptions = new[] {EventSubscription.All};
            blankTeam.FplBotSlackChannel = "#fpltest";
            
            var players = await _playerclient.GetAllPlayers();
            var teams = await _teamsClient.GetAllTeams();
            await _eventsHandler.HandleForTeam(currentGameweek:7, CreateDummyEvents(teams, players), blankTeam, players, teams);
            return Ok();
        }

        private IEnumerable<FixtureEvents> CreateDummyEvents(ICollection<Team> teams, ICollection<Player> players)
        {
            var homeTeam = teams.First(t => t.Name == "Leeds");
            var awayTeam = teams.First(t => t.Name == "Leicester");

            var goalScorer = players.First(p => p.FirstName == "Harvey"  && p.SecondName == "Barnes");
            var assist = players.First(p => p.FirstName == "Jamie" && p.SecondName == "Vardy");

 
            return new[] {new FixtureEvents
            {
                GameScore = new GameScore
                {
                    HomeTeamId = (int)homeTeam.Id,
                    HomeTeamScore = 1,
                    AwayTeamId = (int)awayTeam.Id,
                    AwayTeamScore = 0,
                },
                StatMap = new Dictionary<StatType, List<PlayerEvent>>
                {
                    { StatType.GoalsScored, new List<PlayerEvent> { new PlayerEvent(goalScorer.Id, PlayerEvent.TeamType.Home, false) } },
                    { StatType.Assists, new List<PlayerEvent> { new PlayerEvent(assist.Id, PlayerEvent.TeamType.Home, false) } }
                }
            }};
        }
    }
}