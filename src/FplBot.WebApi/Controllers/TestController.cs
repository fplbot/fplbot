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
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ISlackTeamRepository _teamRepo;
        private readonly IGlobalSettingsClient _settings;
        private readonly FixtureEventsHandler _eventsHandler;
        private readonly InjuryUpdateHandler _statusHandler;
        private readonly LineupReadyHandler _readyHandler;
        private readonly FixtureStateHandler _fixtureStateHandler;
        private readonly IGetMatchDetails _matchDetailsFetcher;
        private readonly IFixtureClient _fixtureClient;
        private readonly ILoggerFactory _factory;
        private ITeamsClient _teamsClient;
        private ISlackWorkSpacePublisher _slackPublisher;
        private NearDeadlineHandler _nearDeadlineHandler;
        private readonly IGameweekClient _gwClient;

        public TestController(ISlackTeamRepository teamRepo, IGlobalSettingsClient settings,
            FixtureEventsHandler eventsHandler, InjuryUpdateHandler statusHandler, LineupReadyHandler readyHandler,
            FixtureStateHandler fixtureStateHandler, IGetMatchDetails matchDetailsFetcher, IFixtureClient fixtureClient, ILoggerFactory factory, ITeamsClient teamsClient, 
            ISlackWorkSpacePublisher slackPublisher, NearDeadlineHandler nearDeadlineHandler, IGameweekClient gwClient)
        {
            _teamRepo = teamRepo;
            _settings = settings;
            _eventsHandler = eventsHandler;
            _statusHandler = statusHandler;
            _readyHandler = readyHandler;
            _fixtureStateHandler = fixtureStateHandler;
            _matchDetailsFetcher = matchDetailsFetcher;
            _fixtureClient = fixtureClient;
            _factory = factory;
            _teamsClient = teamsClient;
            _slackPublisher = slackPublisher;
            _nearDeadlineHandler = nearDeadlineHandler;
            _gwClient = gwClient;
        }

        [HttpGet("neardeadline/{gwId}")]
        public async Task NearDeadline(int gwId, string onehour)
        {
            var gameweek = (await _gwClient.GetGameweeks()).First(gw => gw.Id == gwId);
            
            if(!string.IsNullOrEmpty(onehour))
                await _nearDeadlineHandler.HandleOneHourToDeadline(gameweek);
            else
            {
                await _nearDeadlineHandler.HandleTwentyFourHoursToDeadline(gameweek);
            }
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
        
        [HttpGet("fulltime/{gameweek}")]
        public async Task<IActionResult> GetFixtureFulltime(int gameweek)
        {
            var settings = await _settings.GetGlobalSettings();
            var oldFixtures = await _fixtureClient.GetFixturesByGameweek(gameweek);
            foreach (var oldFixture in oldFixtures)
            {
                oldFixture.FinishedProvisional = false;
            }
            var newFixtures = await _fixtureClient.GetFixturesByGameweek(gameweek);

            var newStuff = LiveEventsExtractor.GetProvisionalFinishedFixtures(newFixtures, oldFixtures, settings.Teams, settings.Players); 
            await _fixtureStateHandler.OnFixturesProvisionalFinished(newStuff);
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

    public class LoggerPublisher : ISlackWorkSpacePublisher
    {
        public Task PublishToAllWorkspaceChannels(string msg)
        {
            Console.WriteLine("======");
            Console.WriteLine(msg);
            Console.WriteLine("======");
            return Task.CompletedTask;
        }

        public Task PublishToAllWorkspaceChannelsWithThreadMessage(string msg, string threadMessage)
        {
            throw new NotImplementedException();
        }

        public Task PublishToWorkspace(string teamId, string channel, params string[] messages)
        {
            throw new NotImplementedException();
        }

        public Task PublishToWorkspace(string teamId, params ChatPostMessageRequest[] message)
        {
            throw new NotImplementedException();
        }
    }
}