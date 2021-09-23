using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers
{
    public static class FixtureDiffer
    {
        public static Dictionary<StatType, List<PlayerEvent>> DiffFixtureStats(Fixture newFixture, Fixture oldFixture, ICollection<Player> players)
        {
            var newFixtureStats = new Dictionary<StatType, List<PlayerEvent>>();

            if (newFixture == null || oldFixture == null)
                return newFixtureStats;

            foreach (var stat in newFixture.Stats)
            {
                var type = StatHelper.FromStatString(stat.Identifier);
                if (type == StatType.Unknown)
                {
                    continue;
                }

                var oldStat = oldFixture.Stats.FirstOrDefault(s => s.Identifier == stat.Identifier);
                var newPlayerEvents = DiffStat(stat, oldStat, players);

                if (newPlayerEvents.Any())
                {
                    newFixtureStats.Add(type, newPlayerEvents);
                }
            }

            return newFixtureStats;
        }

         private static List<PlayerEvent> DiffStat(FixtureStat newStat, FixtureStat oldStat, ICollection<Player> players)
        {
            var diffs = new List<PlayerEvent>();

            diffs.AddRange(DiffStat(TeamType.Home, newStat.HomeStats, oldStat?.HomeStats, players));
            diffs.AddRange(DiffStat(TeamType.Away, newStat.AwayStats, oldStat?.AwayStats, players));

            return diffs;
        }

        private static List<PlayerEvent> DiffStat(
            TeamType teamType,
            ICollection<FixtureStatValue> newStats,
            ICollection<FixtureStatValue> oldStats,
            ICollection<Player> players)
        {
            var diffs = new List<PlayerEvent>();

            foreach (var newStat in newStats)
            {
                var oldStat = oldStats?.FirstOrDefault(old => old.Element == newStat.Element);
                var player = players.FirstOrDefault(p => p.Id == newStat.Element);
                // Player had no stats from last check, so we add as new stat
                if (oldStat == null)
                {
                    diffs.Add(new PlayerEvent(new (player.Id, player.FirstName, player.SecondName, player.WebName), teamType, false));
                    continue;
                }

                // Old player stat is same as new, so we skip it
                if (newStat.Value == oldStat.Value)
                {
                    continue;
                }

                // New stat for player is higher than old stat, so we add as new stat
                if (newStat.Value > oldStat.Value)
                {
                    diffs.Add(new PlayerEvent(new (player.Id, player.FirstName, player.SecondName, player.WebName), teamType, false));
                    continue;
                }

                // New stat for player is lower than old stat, so we add as removed stat
                if (newStat.Value < oldStat.Value)
                {
                    diffs.Add(new PlayerEvent(new (player.Id, player.FirstName, player.SecondName, player.WebName), teamType, true));
                }
            }

            if (oldStats != null)
            {
                foreach (var oldStat in oldStats)
                {
                    var player = players.FirstOrDefault(p => p.Id == oldStat.Element);
                    var newStat = newStats.FirstOrDefault(x => x.Element == oldStat.Element);

                    // Player had a stat previously that is now removed, so we add as removed stat
                    if (newStat == null)
                    {
                        diffs.Add(new PlayerEvent(new (player.Id, player.FirstName, player.SecondName, player.WebName), teamType, true));
                    }
                }
            }

            return diffs;
        }
    }
}
