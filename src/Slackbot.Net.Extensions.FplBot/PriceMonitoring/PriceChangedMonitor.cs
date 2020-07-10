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
        private ICollection<Player> _currentPlayers;

        public PriceChangedMonitor(IPlayerClient playerClient)
        {
            _playerClient = playerClient;
            _currentPlayers = new List<Player>();
        }
        
        public async Task<PriceChanged> GetChangedPlayers()
        {
            if (!_currentPlayers.Any())
            {
                var allPlayersInitial = await _playerClient.GetAllPlayers();
                _currentPlayers = allPlayersInitial;
                var noPlayers = new List<Player>();
                return new PriceChanged(noPlayers);
            }

            var after = await _playerClient.GetAllPlayers();
            var playersWithPriceChanges = after.Where(p => p.CostChangeEvent != 0).ToList();
            var newPlayersWithPriceChanges = playersWithPriceChanges.Except(_currentPlayers, new PlayerPriceComparer()).ToList();
            _currentPlayers = after;
            return new PriceChanged(newPlayersWithPriceChanges);
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