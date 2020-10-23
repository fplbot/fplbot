using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class State : IState
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly ISlackTeamRepository _slackTeamRepo;
      
        private readonly ITransfersByGameWeek _transfersByGameWeek;
        private readonly ILeagueEntriesByGameweek _leagueEntriesByGameweek;
        private readonly ISlackClientBuilder _service;
        private readonly ILogger<State> _logger;


        private ICollection<Team> _teams;
        private readonly IDictionary<long, IEnumerable<TransfersByGameWeek.Transfer>> _transfersForCurrentGameweekBySlackTeam;
        private readonly IDictionary<long, IEnumerable<GameweekEntry>> _entriesForCurrentGameweekBySlackTeam;
        private ICollection<Player> _players;
        private ICollection<Fixture> _currentGameweekFixtures;
        private readonly IDictionary<string, IEnumerable<User>> _slackUsers;
        private readonly IList<SlackTeam> _activeSlackTeams;
        private int? _currentGameweek;


        public State(IFixtureClient fixtureClient, 
            IPlayerClient playerClient, 
            ITeamsClient teamsClient,
            ISlackTeamRepository slackTeamRepo,
            ITransfersByGameWeek transfersByGameWeek,
            ILeagueEntriesByGameweek leagueEntriesByGameweek,
            ISlackClientBuilder service,
            ILogger<State> logger)
        {
            _fixtureClient = fixtureClient;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _slackTeamRepo = slackTeamRepo;
            _transfersByGameWeek = transfersByGameWeek;
            _leagueEntriesByGameweek = leagueEntriesByGameweek;
            _service = service;
            _logger = logger;

            _transfersForCurrentGameweekBySlackTeam = new Dictionary<long, IEnumerable<TransfersByGameWeek.Transfer>>();
            _entriesForCurrentGameweekBySlackTeam = new Dictionary<long, IEnumerable<GameweekEntry>>();
            _currentGameweekFixtures = new List<Fixture>();
            _players = new List<Player>();
            _teams = new List<Team>();
            _slackUsers = new Dictionary<string, IEnumerable<User>>();
            _activeSlackTeams = new List<SlackTeam>();
        }

        public async Task Reset(int newGameweek)
        {
            _currentGameweek = newGameweek;
            _currentGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(newGameweek);
            _players = await _playerClient.GetAllPlayers();
            _teams = await _teamsClient.GetAllTeams();
            _transfersForCurrentGameweekBySlackTeam.Clear();
            _entriesForCurrentGameweekBySlackTeam.Clear();
            _slackUsers.Clear();
            _activeSlackTeams.Clear();
            await EnsureNewLeaguesAreMonitored(newGameweek);
        }

        public IEnumerable<SlackTeam> GetActiveTeams()
        {
            return _activeSlackTeams;
        }

        public async Task<IEnumerable<FixtureEvents>> Refresh(int currentGameweek)
        {
            await EnsureNewLeaguesAreMonitored(currentGameweek);
            
            var latest = await _fixtureClient.GetFixturesByGameweek(currentGameweek);
            var fixtureEvents = LiveEventsExtractor.GetUpdatedFixtureEvents(latest, _currentGameweekFixtures);
            
            if (fixtureEvents.Any())
            {
                _currentGameweekFixtures = latest;
            }
            _logger.LogInformation($"Active teams count: {_activeSlackTeams.Count}");
            _logger.LogInformation($"Slack users count: {_slackUsers.Count}");
            _logger.LogInformation($"Transfers count: {_transfersForCurrentGameweekBySlackTeam.Count}");

            return fixtureEvents;
        }

        public GameweekLeagueContext GetGameweekLeagueContext(string teamId)
        {
            var slackTeam = _activeSlackTeams.FirstOrDefault(t => t.TeamId == teamId);
            var transfersForLeague = Enumerable.Empty<TransfersByGameWeek.Transfer>();
            var entriesForLeague = Enumerable.Empty<GameweekEntry>();
            var eventSubscriptions = Enumerable.Empty<EventSubscription>();

            if (slackTeam != null)
            {
                _transfersForCurrentGameweekBySlackTeam.TryGetValue(slackTeam.FplbotLeagueId, out transfersForLeague);
                _entriesForCurrentGameweekBySlackTeam.TryGetValue(slackTeam.FplbotLeagueId, out entriesForLeague);
                eventSubscriptions = slackTeam.Subscriptions;
            }

            return new GameweekLeagueContext
            {
                Players = _players,
                Teams = _teams,
                GameweekEntries = entriesForLeague,
                TransfersForLeague = transfersForLeague,
                Users = _slackUsers.ContainsKey(teamId) ? _slackUsers[teamId] : new List<User>(),
                EventSubscriptions = eventSubscriptions,
                CurrentGameweek = _currentGameweek
            };
        }

        private async Task EnsureNewLeaguesAreMonitored(int currentGameweek)
        {
            _currentGameweek = currentGameweek;
            var allTeams = await _slackTeamRepo.GetAllTeams();
            foreach (var t in allTeams)
            {
                if (!_transfersForCurrentGameweekBySlackTeam.ContainsKey(t.FplbotLeagueId))
                {
                    var transfersForLeague = await _transfersByGameWeek.GetTransfersByGameweek(currentGameweek, (int)t.FplbotLeagueId);
                    _transfersForCurrentGameweekBySlackTeam.Add((int) t.FplbotLeagueId, transfersForLeague);
                }

                if (!_entriesForCurrentGameweekBySlackTeam.ContainsKey(t.FplbotLeagueId))
                {
                    var entriesForLeagues = await _leagueEntriesByGameweek.GetEntriesForGameweek(currentGameweek, (int)t.FplbotLeagueId);
                    _entriesForCurrentGameweekBySlackTeam.Add((int)t.FplbotLeagueId, entriesForLeagues);
                }

                var slackClient = _service.Build(t.AccessToken);

                try
                {
                    var users = await slackClient.UsersList();
                    if (users != null && users.Ok)
                    {
                        if (_slackUsers.ContainsKey(t.TeamId))
                        {
                            _slackUsers.Remove(t.TeamId);
                        }
                        _slackUsers.Add(t.TeamId, users.Members);    
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                    _slackUsers.Add(t.TeamId, new List<User>());
                }

                if (!_activeSlackTeams.Any(team => team.TeamId == t.TeamId))
                {
                    _activeSlackTeams.Add(t);    
                }
            }
        }
    }
}