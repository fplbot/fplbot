using System;
using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using Fpl.Workers.Extensions;
using Fpl.Workers.Models.Comparers;
using FplBot.Messaging.Contracts.Events.v1;

namespace Fpl.Workers.Models.Mappers
{
    public class LiveEventsExtractor
    {
        public static IEnumerable<FixtureEvents> GetUpdatedFixtureEvents(ICollection<Fixture> latestFixtures, ICollection<Fixture> current, ICollection<Player> players, ICollection<Team> teams)
        {
            if(latestFixtures == null)
                return new List<FixtureEvents>();

            if (current == null)
                return new List<FixtureEvents>();

            return latestFixtures.Where(f => f.Stats.Any()).Select(fixture =>
            {
                var homeTeam = teams.FirstOrDefault(t => t.Id == fixture.HomeTeamId);
                var awayTeam = teams.FirstOrDefault(t => t.Id == fixture.AwayTeamId);
                var oldFixture = current.FirstOrDefault(f => f.Code == fixture.Code);
                if (oldFixture != null)
                {
                    var newFixtureStats = FixtureDiffer.DiffFixtureStats(fixture, oldFixture, players);

                    if (newFixtureStats.Values.Any())
                        return new FixtureEvents
                        (
                            new FixtureScore(

                                new FixtureTeam(homeTeam.Id, homeTeam.Name, homeTeam.ShortName),
                                new FixtureTeam(awayTeam.Id, awayTeam.Name, awayTeam.ShortName),
                                fixture.HomeTeamScore,
                                fixture.AwayTeamScore
                            ),
                            newFixtureStats
                        );
                    else
                        return null;
                }

                return null;
            }).WhereNotNull();
        }

        public static IEnumerable<int> GetProvisionalFinishedFixtures(ICollection<Fixture> latestFixtures, ICollection<Fixture> current, ICollection<Team> teams, ICollection<Player> players)
        {
            if(latestFixtures == null)
                return new List<int>();

            if (current == null)
                return new List<int>();

            var latestFinished = latestFixtures.Where(f => f.FinishedProvisional);
            var currentFinished = current.Where(f => f.FinishedProvisional);
            var newFinished = latestFinished.Except(currentFinished, new FixtureComparer());
            if (newFinished.Any())
                return newFinished.Select(n => n.Id);
            return new List<int>();
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
