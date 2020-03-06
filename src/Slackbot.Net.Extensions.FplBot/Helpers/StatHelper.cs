using Fpl.Client.Models;
using System.Collections.Generic;
using System.Linq;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    public static class StatHelper
    {
        public static IDictionary<StatType, List<PlayerEvent>> DiffFixtureStats(Fixture newFixture, Fixture oldFixture)
        {
            var newFixtureStats = new Dictionary<StatType, List<PlayerEvent>>();

            foreach (var stat in newFixture.Stats)
            {
                var type = StatTypeMethods.FromStatString(stat.Identifier);
                if (type == StatType.Unknown)
                {
                    continue;
                }

                var oldStat = oldFixture.Stats.FirstOrDefault(s => s.Identifier == stat.Identifier);
                var newPlayerEvents = DiffStat(stat, oldStat);

                if (newPlayerEvents.Any())
                {
                    newFixtureStats.Add(type, newPlayerEvents);
                }
            }

            return newFixtureStats;
        }

        private static List<PlayerEvent> DiffStat(FixtureStat newStat, FixtureStat oldStat)
        {
            var diffs = new List<PlayerEvent>();

            diffs.AddRange(DiffStat(PlayerEvent.TeamType.Home, newStat.HomeStats, oldStat.HomeStats));
            diffs.AddRange(DiffStat(PlayerEvent.TeamType.Away, newStat.AwayStats, oldStat.AwayStats));

            return diffs;
        }

        private static List<PlayerEvent> DiffStat(
            PlayerEvent.TeamType teamType,
            ICollection<FixtureStatValue> newStats,
            ICollection<FixtureStatValue> oldStats)
        {
            var diffs = new List<PlayerEvent>();

            foreach (var newStat in newStats)
            {
                var oldStat = oldStats.FirstOrDefault(old => old.Element == newStat.Element);

                // Player had no stats from last check, so we add as new stat
                if (oldStat == null)
                {
                    diffs.Add(new PlayerEvent(newStat.Element, teamType, false));
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
                    diffs.Add(new PlayerEvent(newStat.Element, teamType, false));
                    continue;
                }

                // New stat for player is lower than old stat, so we add as removed stat
                if (newStat.Value < oldStat.Value)
                {
                    diffs.Add(new PlayerEvent(newStat.Element, teamType, true));
                }
            }

            foreach (var oldStat in oldStats)
            {
                var newStat = newStats.FirstOrDefault(x => x.Element == oldStat.Element);

                // Player had a stat previously that is now removed, so we add as removed stat
                if (newStat == null)
                {
                    diffs.Add(new PlayerEvent(oldStat.Element, teamType, true));
                }
            }

            return diffs;
        }
    }
}
