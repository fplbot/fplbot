using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.Extensions.FplBot.PriceMonitoring;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    public class PlayerChangesEventsExtractor
    {
        
        public static IEnumerable<PlayerUpdate> GetPriceChanges(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
        {
            return ComparePlayers(after, players, teams, new PlayerPriceComparer());
        }

        public static IEnumerable<PlayerUpdate> GetStatusChanges(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
        {
            return ComparePlayers(after, players, teams, new StatusComparer());
        }
        
        public static IEnumerable<PlayerUpdate> GetEventTransfers(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
        {
            return ComparePlayers(after, players, teams, new PlayerTransfersComparer());
        }

        private static IEnumerable<PlayerUpdate> ComparePlayers(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams, IEqualityComparer<Player> changeComparer)
        {
            if(players == null)
                return new List<PlayerUpdate>();
            
            if (after == null)
                return new List<PlayerUpdate>();
            
            var playersWithChanges = after.Except(players, changeComparer).ToList();
            var updates = new List<PlayerUpdate>();
            foreach (var player in playersWithChanges)
            {
                var fromPlayer = players.FirstOrDefault(p => p.Id == player.Id);
                updates.Add(new PlayerUpdate
                {
                    FromPlayer = fromPlayer,
                    ToPlayer = player,
                    Team = teams.FirstOrDefault(t => t.Code == player.TeamCode),
                });
            }

            return updates;
        }

      
    }
}