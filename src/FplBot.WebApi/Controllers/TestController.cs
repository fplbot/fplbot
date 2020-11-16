
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Common;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.AspNetCore.Mvc;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle;
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
        private readonly IGlobalSettingsClient _settings;
        private readonly FixtureEventsHandler _eventsHandler;
        private readonly StatusUpdateHandler _statusHandler;
        private readonly LineupReadyHandler _readyHandler;
        private readonly IGetMatchDetails _matchDetailsFetcher;
        private readonly IFixtureClient _fixtureClient;

        public TestController(ISlackTeamRepository teamRepo, IGlobalSettingsClient settings, FixtureEventsHandler eventsHandler, StatusUpdateHandler statusHandler, LineupReadyHandler readyHandler, IGetMatchDetails matchDetailsFetcher, IFixtureClient fixtureClient)
        {
            _teamRepo = teamRepo;
            _settings = settings;
            _eventsHandler = eventsHandler;
            _statusHandler = statusHandler;
            _readyHandler = readyHandler;
            _matchDetailsFetcher = matchDetailsFetcher;
            _fixtureClient = fixtureClient;
        }

        [HttpGet("fixture-event")]
        public async Task<IActionResult> GetTestFixtureEvent()
        {
            var blankTeam = await _teamRepo.GetTeam("T0A9QSU83");
            blankTeam.FplbotLeagueId = 619747;
            blankTeam.Subscriptions = new[] {EventSubscription.All};
            blankTeam.FplBotSlackChannel = "#fpltest";

            var globalSettings = await _settings.GetGlobalSettings();
            var players = globalSettings.Players;
            var teams = globalSettings.Teams;
            var fixtureUpdates = new FixtureUpdates
            {
                CurrentGameweek = 7,
                Players = players,
                Teams = teams,
                Events = CreateDummyEvents(teams, players)
            };
            await _eventsHandler.HandleForTeam(fixtureUpdates, blankTeam);
            return Ok();
        }
        
        [HttpGet("status-event")]
        public async Task<IActionResult> GetStatusUpdate()
        {
            await _statusHandler.OnInjuryUpdates(StatusUpdates());
            return Ok();
        }
        
        [HttpGet("status-event2")]
        public async Task<IActionResult> GetStatusUpdate2()
        {
            var globalSettings = await _settings.GetGlobalSettings();
            var players = globalSettings.Players;
            var teams = globalSettings.Teams;
            foreach (var player in players)
            {
                player.Status = PlayerStatuses.Available;
            }
            
            var afterSettings = await _settings.GetGlobalSettings();

            var after = afterSettings.Players;
            var statusUpdates = PlayerChangesEventsExtractor.GetStatusChanges(after, players, teams);
            await _statusHandler.OnInjuryUpdates(statusUpdates);
            return Ok();
        }

        [HttpGet("lineups/{pulseId}")]
        public async Task<IActionResult> GetLineUps(int pulseId)
        {
            var mDetails = await _matchDetailsFetcher.GetMatchDetails(pulseId);
            var lineups = MatchDetailsMapper.ToLineup(mDetails);
            await _readyHandler.HandleLineupReady(lineups);
            return Ok();
        }
        
        [HttpGet("fulltime")]
        public async Task<IActionResult> GetFixtureFulltime()
        {
            var settings = await _settings.GetGlobalSettings();
            var fixtures = await _fixtureClient.GetFixturesByGameweek(8);
            await _statusHandler.OnFixturesProvisionalFinished(new []
            {
                new FinishedFixture
                {
                    Fixture = fixtures.Last(),
                    HomeTeam = settings.Teams.First(),
                    AwayTeam = settings.Teams.Last()
                },
                new FinishedFixture
                {
                    Fixture = fixtures.GetItemByIndex(2),
                    HomeTeam = settings.Teams.GetItemByIndex(4),
                    AwayTeam = settings.Teams.GetItemByIndex(5)
                }
            });
            return Ok();
        }
        
        private static PlayerUpdate[] StatusUpdates()
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

        private static PlayerUpdate PlayerStatusUpdate(string fromStatus, string toStatus, int rando, string updatedDoubtfulNews = null)
        {
            return new PlayerUpdate
            {
                FromPlayer = new Player
                {
                    WebName = $"{fromStatus}-{toStatus}",
                    FirstName = "Jonzo",
                    SecondName = "Jizzler",
                    Status = fromStatus,
                    News = $"Quacked his toe. 55% chance of playing",
                },
                Team = new Team { ShortName = "FICTIVE FC"},
                ToPlayer = new Player
                {
                    WebName = $"{fromStatus}-{toStatus}",
                    FirstName = "Jonzo",
                    SecondName = "Jizzler",
                    Status = toStatus,
                    News = updatedDoubtfulNews ?? $"Quacked his other {rando} toe. 55% chance of playing",  
                }
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