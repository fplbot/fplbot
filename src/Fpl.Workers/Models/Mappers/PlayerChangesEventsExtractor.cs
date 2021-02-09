using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using FplBot.Core.Helpers.Comparers;
using FplBot.Core.Models;

namespace FplBot.Core.Helpers
{
    public class PlayerChangesEventsExtractor
    {
        
        public static IEnumerable<PlayerUpdate> GetPriceChanges(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
        {
            if(players == null)
                return new List<PlayerUpdate>();
            
            if (after == null)
                return new List<PlayerUpdate>();
            
            return ComparePlayers(after, players, teams, new PlayerPriceComparer());
        }

        public static IEnumerable<PlayerUpdate> GetInjuryUpdates(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
        {
            if(players == null)
                return new List<PlayerUpdate>();
            
            if (after == null)
                return new List<PlayerUpdate>();
            
            return ComparePlayers(after, players, teams, new StatusComparer());
        }

        private static IEnumerable<PlayerUpdate> ComparePlayers(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams, IEqualityComparer<Player> changeComparer)
        {
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