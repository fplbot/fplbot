using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers
{
    internal class State : IState
    {
        private readonly IFixtureClient _fixtureClient;
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly ISlackTeamRepository _slackTeamRepo;
      
        private readonly ITransfersByGameWeek _transfersByGameWeek;


        private ICollection<Team> _teams;
        private readonly IDictionary<long, IEnumerable<TransfersByGameWeek.Transfer>> _transfersForCurrentGameweek;
        private ICollection<Player> _players;
        private ICollection<Fixture> _currentGameweekFixtures;
        

        public State(IFixtureClient fixtureClient, 
            IPlayerClient playerClient, 
            ITeamsClient teamsClient,
            ISlackTeamRepository slackTeamRepo,
            ITransfersByGameWeek transfersByGameWeek)
        {
            _fixtureClient = fixtureClient;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _slackTeamRepo = slackTeamRepo;
            _transfersByGameWeek = transfersByGameWeek;

            _transfersForCurrentGameweek = new Dictionary<long, IEnumerable<TransfersByGameWeek.Transfer>>();
            _currentGameweekFixtures = new List<Fixture>();
            _players = new List<Player>();
            _teams = new List<Team>();
        }

        public async Task Reset(int newGameweek)
        {
            _currentGameweekFixtures = await _fixtureClient.GetFixturesByGameweek(newGameweek);
            _players = await _playerClient.GetAllPlayers();
            _teams = await _teamsClient.GetAllTeams();
            _transfersForCurrentGameweek.Clear();

            await EnsureNewLeaguesAreMonitored(newGameweek);
        }

        public IEnumerable<long> GetLeagues()
        {
            return _transfersForCurrentGameweek.Keys;
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

            return fixtureEvents;
        }

        public GameweekLeagueContext GetGameweekLeagueContext(long league)
        {
            return new GameweekLeagueContext
            {
                Players = _players,
                Teams = _teams,
                TransfersForLeague = _transfersForCurrentGameweek.ContainsKey(league) ? _transfersForCurrentGameweek[league] : new List<TransfersByGameWeek.Transfer>()
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
            }
        }
    }
}