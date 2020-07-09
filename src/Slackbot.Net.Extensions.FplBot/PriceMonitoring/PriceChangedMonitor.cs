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
                _currentPlayers = await _playerClient.GetAllPlayers();
                return new PriceChanged(new List<Player>());
            }

            var after = await _playerClient.GetAllPlayers();
            var playersWithPriceChanges = GetPlayersWithPriceChanges(_currentPlayers, after);
            _currentPlayers = after;
            return new PriceChanged(playersWithPriceChanges);
        }

        private ICollection<Player> GetPlayersWithPriceChanges(ICollection<Player> currentPlayers, ICollection<Player> newPlayers)
        {
            return currentPlayers.Where(p => p.NowCost != newPlayers.First(np => np.Id == p.Id).NowCost).ToList();
        }
    }
}