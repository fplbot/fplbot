using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;
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
        private readonly ISlackClientService _service;
        private readonly ILogger<State> _logger;


        private ICollection<Team> _teams;
        private readonly IDictionary<long, IEnumerable<TransfersByGameWeek.Transfer>> _transfersForCurrentGameweek;
        private ICollection<Player> _players;
        private ICollection<Fixture> _currentGameweekFixtures;
        private readonly IDictionary<string, IEnumerable<User>> _slackUsers;
        private readonly IList<SlackTeam> _activeTeams;


        public State(IFixtureClient fixtureClient, 
            IPlayerClient playerClient, 
            ITeamsClient teamsClient,
            ISlackTeamRepository slackTeamRepo,
            ITransfersByGameWeek transfersByGameWeek,
            ISlackClientService service,
            ILogger<State> logger)
        {
            _fixtureClient = fixtureClient;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _slackTeamRepo = slackTeamRepo;
            _transfersByGameWeek = transfersByGameWeek;
            _service = service;
            _logger = logger;

            _transfersForCurrentGameweek = new Dictionary<long, IEnumerable<TransfersByGameWeek.Transfer>>();
            _currentGameweekFixtures = new List<Fixture>();
            _players = new List<Player>();
            _teams = new List<Team>();
            _slackUsers = new Dictionary<string, IEnumerable<User>>();
            _activeTeams = new List<SlackTeam>();
        }

        public async Task Reset(int newGameweek)
        {
            _currentGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(newGameweek);
            _players = await _playerClient.GetAllPlayers();
            _teams = await _teamsClient.GetAllTeams();
            _transfersForCurrentGameweek.Clear();
            _slackUsers.Clear();
            _activeTeams.Clear();
            await EnsureNewLeaguesAreMonitored(newGameweek);
        }

        public IEnumerable<SlackTeam> GetActiveTeams()
        {
            return _activeTeams;
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
            _logger.LogInformation($"Active teams count: {_activeTeams.Count}");
            _logger.LogInformation($"Slack users count: {_slackUsers.Count}");
            _logger.LogInformation($"Transfers count: {_transfersForCurrentGameweek.Count}");

            return fixtureEvents;
        }

        public GameweekLeagueContext GetGameweekLeagueContext(string teamId)
        {
            var teamForLeague = _activeTeams.FirstOrDefault(t => t.TeamId == teamId);
            IEnumerable<TransfersByGameWeek.Transfer> transfersForLeague = new List<TransfersByGameWeek.Transfer>();
            
            if (teamForLeague != null && _transfersForCurrentGameweek.ContainsKey(teamForLeague.FplbotLeagueId))
                transfersForLeague = _transfersForCurrentGameweek[teamForLeague.FplbotLeagueId]; 
            
            return new GameweekLeagueContext
            {
                Players = _players,
                Teams = _teams,
                TransfersForLeague = transfersForLeague,
                Users = _slackUsers.ContainsKey(teamId) ? _slackUsers[teamId] : new List<User>()
            };
        }

        private async Task EnsureNewLeaguesAreMonitored(int currentGameweek)
        {
            var allTeams = await _slackTeamRepo.GetAllTeamsAsync();
            foreach (var t in allTeams)
            {
                if (!_transfersForCurrentGameweek.ContainsKey(t.FplbotLeagueId))
                {
                    var transferForLeague = await _transfersByGameWeek.GetTransfersByGameweek(currentGameweek, (int)t.FplbotLeagueId);
                    _transfersForCurrentGameweek.Add((int) t.FplbotLeagueId, transferForLeague);
                }
                
                var slackClient = await _service.CreateClient(t.TeamId);

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

                if (!_activeTeams.Any(team => team.TeamId == t.TeamId))
                {
                    _activeTeams.Add(t);    
                }
            }
        }
    }
}