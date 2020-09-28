using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Slackbot.Net.Extensions.FplBot.PriceMonitoring
{
    public class PriceChangedMonitor
    {
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private ICollection<Player> _currentPlayers;
        private ICollection<Team> _currentTeams;

        public PriceChangedMonitor(IPlayerClient playerClient, ITeamsClient teamsClient)
        {
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _currentPlayers = new List<Player>();
            _currentTeams = new List<Team>(); 
        }
        
        public async Task<PriceChanged> GetChangedPlayers()
        {
            if (!_currentTeams.Any())
            {
                var allTeamsInitial = await _teamsClient.GetAllTeams();
                _currentTeams = allTeamsInitial;
            }
            if (!_currentPlayers.Any())
            {
                var allPlayersInitial = await _playerClient.GetAllPlayers();
                _currentPlayers = allPlayersInitial;
                var noPlayers = new List<Player>();
                return new PriceChanged(noPlayers, _currentTeams);
            }

            var after = await _playerClient.GetAllPlayers();
            var playersWithPriceChanges = after.Where(p => p.CostChangeEvent != 0).ToList();
            var newPlayersWithPriceChanges = playersWithPriceChanges.Except(_currentPlayers, new PlayerPriceComparer()).ToList();
            _currentPlayers = after;
            return new PriceChanged(newPlayersWithPriceChanges, _currentTeams);
        }
    }

    public class PlayerPriceComparer : IEqualityComparer<Player>
    {
        public bool Equals(Player x, Player y)
        {
            if (x == null && y == null)
                return true;
            
            if (x == null || y == null)
                return false;
            
            return x.Id == y.Id && x.CostChangeEvent == y.CostChangeEvent;
        }

        public int GetHashCode(Player obj)
        {
            return obj.Id;
        }
    }
}