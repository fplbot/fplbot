using System;
using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using FplBot.Core.Extensions;
using FplBot.Core.Models;

namespace FplBot.Core.Helpers
{
    public class LiveEventsExtractor
    {
        public static IEnumerable<FixtureEvents> GetUpdatedFixtureEvents(ICollection<Fixture> latestFixtures, ICollection<Fixture> current)
        {
            if(latestFixtures == null)
                return new List<FixtureEvents>();
            
            if (current == null)
                return new List<FixtureEvents>();
            
            return latestFixtures.Where(f => f.Stats.Any()).Select(fixture =>
            {
                var oldFixture = current.FirstOrDefault(f => f.Code == fixture.Code);
                if (oldFixture != null)
                {
                    var newFixtureStats = StatHelper.DiffFixtureStats(fixture, oldFixture);

                    if (newFixtureStats.Values.Any())
                        return new FixtureEvents
                        {
                            GameScore = new GameScore
                            {
                                HomeTeamId = fixture.HomeTeamId,
                                AwayTeamId = fixture.AwayTeamId,
                                HomeTeamScore = fixture.HomeTeamScore,
                                AwayTeamScore = fixture.AwayTeamScore,
                            },
                            StatMap = newFixtureStats
                        };
                    else
                        return null;
                }

                return null;
            }).WhereNotNull();
        }
        
        public static IEnumerable<FinishedFixture> GetProvisionalFinishedFixtures(ICollection<Fixture> latestFixtures, ICollection<Fixture> current, ICollection<Team> teams, ICollection<Player> players)
        {
            if(latestFixtures == null)
                return new List<FinishedFixture>();
            
            if (current == null)
                return new List<FinishedFixture>();

            var latestFinished = latestFixtures.Where(f => f.FinishedProvisional);
            var currentFinished = current.Where(f => f.FinishedProvisional);
            var newFinished = latestFinished.Except(currentFinished, new FixtureComparer());
            if (newFinished.Any())
                return newFinished.Select(n => new FinishedFixture
                {
                    Fixture = n,
                    HomeTeam = teams.First(t => t.Id == n.HomeTeamId),
                    AwayTeam = teams.First(t => t.Id == n.AwayTeamId),
                    BonusPoints = CreateBonusPlayers(players, n)
                });
            return new List<FinishedFixture>();
        }

        public static IEnumerable<BonusPointsPlayer> CreateBonusPlayers(ICollection<Player> players, Fixture fixture)
        {
            try
            {
                var bonusPointsHome = fixture.Stats.FirstOrDefault(s => s.Identifier == "bps")?.HomeStats;
                var bonusPointsAway = fixture.Stats.FirstOrDefault(s => s.Identifier == "bps")?.AwayStats;

                var home = bonusPointsHome.Select(BpsFilter).ToList();
                var away = bonusPointsAway.Select(BpsFilter).ToList();
                var aggregated = home.Concat(away).OrderByDescending(bpp => bpp.BonusPoints);
                return aggregated;

                BonusPointsPlayer BpsFilter(FixtureStatValue bps)
                {
                    return new BonusPointsPlayer
                    {
                        Player = players.First(p => p.Id == bps.Element),
                        BonusPoints = bps.Value
                    };
                }
            }
            catch
            {
                return new List<BonusPointsPlayer>();
            }
        }
    }

    internal class FixtureComparer : IEqualityComparer<Fixture>
    {
        public bool Equals(Fixture x, Fixture y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Id == y.Id && x.Code == y.Code;
        }

        public int GetHashCode(Fixture obj)
        {
            return HashCode.Combine(obj.Id, obj.Code);
        }
    }
}