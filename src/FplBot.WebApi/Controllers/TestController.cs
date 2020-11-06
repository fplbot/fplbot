
using System;
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
using Slackbot.Net.Extensions.FplBot.Helpers;
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
        private readonly StatusUpdateHandler _statusHandler;

        public TestController(ISlackTeamRepository teamRepo, ILeagueClient leagueClient, IPlayerClient playerclient, ITeamsClient teamsClient, FixtureEventsHandler eventsHandler, StatusUpdateHandler statusHandler, ILogger<FplController> logger)
        {
            _teamRepo = teamRepo;
            _leagueClient = leagueClient;
            _playerclient = playerclient;
            _teamsClient = teamsClient;
            _eventsHandler = eventsHandler;
            _statusHandler = statusHandler;
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
        
        [HttpGet("status-event")]
        public async Task<IActionResult> GetStatusUpdate()
        {
            await _statusHandler.OnStatusUpdates(StatusUpdates());
            return Ok();
        }
        
        [HttpGet("status-event2")]
        public async Task<IActionResult> GetStatusUpdate2()
        {
            var all = await _playerclient.GetAllPlayers();
            foreach (var player in all)
            {
                player.Status = PlayerStatuses.Available;
            }
            var after = await _playerclient.GetAllPlayers();
            var teams = await _teamsClient.GetAllTeams();
            var statusUpdates = PlayerChangesEventsExtractor.GetStatusChanges(after, all, teams);
            await _statusHandler.OnStatusUpdates(statusUpdates);
            return Ok();
        }

        private static PlayerStatusUpdate[] StatusUpdates()
        {
            var random = new Random();
            return new []
            {
                PlayerStatusUpdate(PlayerStatuses.Available, PlayerStatuses.Injured, random.Next(0,100)),
                PlayerStatusUpdate(null, PlayerStatuses.Injured, random.Next(0,100)),
                PlayerStatusUpdate(null, PlayerStatuses.Available, random.Next(0,100)),
                PlayerStatusUpdate(PlayerStatuses.Available, PlayerStatuses.Injured, random.Next(0,100)),
                PlayerStatusUpdate(PlayerStatuses.Injured, PlayerStatuses.Available, random.Next(0,100)),
                PlayerStatusUpdate(PlayerStatuses.Injured, PlayerStatuses.Injured, random.Next(0,100)),
                PlayerStatusUpdate(PlayerStatuses.Injured, PlayerStatuses.Injured, random.Next(0,100)),
                PlayerStatusUpdate(PlayerStatuses.Suspended, PlayerStatuses.Suspended, random.Next(0,100)),
                PlayerStatusUpdate(PlayerStatuses.Suspended, PlayerStatuses.Doubtful, random.Next(0,100)),
                PlayerStatusUpdate(PlayerStatuses.Doubtful, PlayerStatuses.Doubtful, random.Next(0,100), "Knock - 75% chance of playing"),
                PlayerStatusUpdate(PlayerStatuses.Doubtful, PlayerStatuses.Doubtful, random.Next(0,100), "Knock - 25% chance of playing"),
                PlayerStatusUpdate(PlayerStatuses.Doubtful, PlayerStatuses.Doubtful, random.Next(0,100), "self-isolating from the china virus"),
            };
        }

        private static PlayerStatusUpdate PlayerStatusUpdate(string fromStatus, string toStatus, int rando, string updatedDoubtfulNews = null)
        {
            return new PlayerStatusUpdate
            {
                PlayerWebName = $"{fromStatus}-{toStatus}",
                PlayerFirstName = "Jonzo",
                PlayerSecondName = "Jizzler",
                TeamName = "FICTIVE FC",
                FromStatus = fromStatus,
                FromNews = $"Quacked his toe. 55% chance of playing",
                ToStatus = toStatus,
                ToNews = updatedDoubtfulNews ?? $"Quacked his other {rando} toe. 55% chance of playing",
            };
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