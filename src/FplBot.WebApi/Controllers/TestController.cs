
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IPlayerClient _playerclient;
        private readonly ITeamsClient _teamsClient;
        private readonly FixtureEventsHandler _eventsHandler;
        private readonly StatusUpdateHandler _statusHandler;
        private readonly LineupReadyHandler _readyHandler;
        private readonly IGetMatchDetails _matchDetailsFetcher;

        public TestController(ISlackTeamRepository teamRepo, IPlayerClient playerclient, ITeamsClient teamsClient, FixtureEventsHandler eventsHandler, StatusUpdateHandler statusHandler, LineupReadyHandler readyHandler, IGetMatchDetails matchDetailsFetcher)
        {
            _teamRepo = teamRepo;
            _playerclient = playerclient;
            _teamsClient = teamsClient;
            _eventsHandler = eventsHandler;
            _statusHandler = statusHandler;
            _readyHandler = readyHandler;
            _matchDetailsFetcher = matchDetailsFetcher;
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

        [HttpGet("lineups/{pulseId}")]
        public async Task<IActionResult> GetLineUps(int pulseId)
        {
            var mDetails = await _matchDetailsFetcher.GetMatchDetails(pulseId);
            var lineups = MatchDetailsMapper.ToLineup(mDetails);
            await _readyHandler.HandleLineupReady(lineups);
            return Ok();
        }
        
        [HttpGet("lineups-mock}")]
        public async Task<IActionResult> GetLineUpsMocked()
        {
            await _readyHandler.HandleLineupReady(SomeLineups());
            return Ok();
        }

        private Lineups SomeLineups()
        {
            return new Lineups
            {
                FixturePulseId = 123,
                HomeTeamNameAbbr = "LFC",
                AwayTeamNameAbbr = "AFC",
                HomeTeamLineup = SomeLineupSheet(),
                AwayTeamLineup = SomeLineupSheet()
            };
        }

        private IEnumerable<PlayerInLineup> SomeLineupSheet()
        {
            yield return SomePlayer("Keeper", "Keepy", "Keepersen", PlayerInLineup.MatchPositionGoalie);
            yield return SomePlayer("McDeffy1", "Def1", "Deffy1", PlayerInLineup.MatchPositionDefender);
            yield return SomePlayer("McDeffy2", "Def2", "Deffy2", PlayerInLineup.MatchPositionDefender);
            yield return SomePlayer("McDeffy3", "Def3", "Deffy3", PlayerInLineup.MatchPositionDefender);
            yield return SomePlayer("McDeffy4", "Def4", "Deffy4", PlayerInLineup.MatchPositionDefender);
            yield return SomePlayer("Midfielderinho1", "Jesus", "Mid1", PlayerInLineup.MatchPositionMidfielder);
            yield return SomePlayer("Midfielderinho2", "Jesus", "Mid2", PlayerInLineup.MatchPositionMidfielder);
            yield return SomePlayer("Midfielderinho3", "Jesus", "Mid3", PlayerInLineup.MatchPositionMidfielder);
            yield return SomePlayer("Forwart1", "Jacob", "For1", PlayerInLineup.MatchPositionForward);
            yield return SomePlayer("Forwart2", "Jacob", "For2", PlayerInLineup.MatchPositionForward);
            yield return SomePlayer("Forwart3", "Jacob", "For3", PlayerInLineup.MatchPositionForward);
        }

        private static PlayerInLineup SomePlayer(string displayName, string first, string last, string pos)
        {
            return new PlayerInLineup { Name = new Name { Display = displayName, First = first, Last = last}, Captain = false, MatchPosition = pos};
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
                FromPlayer = new Player
                {
                    WebName = $"{fromStatus}-{toStatus}",
                    FirstName = "Jonzo",
                    SecondName = "Jizzler",
                    Status = fromStatus,
                    News = $"Quacked his toe. 55% chance of playing",
                },
                TeamName = "FICTIVE FC",
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