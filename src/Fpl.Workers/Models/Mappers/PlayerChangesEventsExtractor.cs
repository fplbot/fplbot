using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using FplBot.Core.Helpers.Comparers;
using FplBot.Core.Models;
using FplBot.Messaging.Contracts.Events.v1;

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

        public static IEnumerable<NewPlayer> GetNewPlayers(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
        {
            if (players == null)
                return new List<NewPlayer>();
            if (after == null)
                return new List<NewPlayer>();

            var diff = after.Except(players, new PlayerIdComparer());

            if (!diff.Any())
                return new List<NewPlayer>();

            var updates = diff.Select(newPlayer => new NewPlayer
            {
                PlayerId = newPlayer.Id,
                WebName = newPlayer.WebName,
                NowCost = newPlayer.NowCost,
                TeamId = teams.FirstOrDefault(t => t.Code == newPlayer.TeamCode).Id,
                TeamShortName = teams.FirstOrDefault(t => t.Code == newPlayer.TeamCode).Name
            });
            return updates;
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
