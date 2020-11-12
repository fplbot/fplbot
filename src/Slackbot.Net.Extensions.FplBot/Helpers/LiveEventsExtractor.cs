using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Models;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class LiveEventsExtractor
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
        
        public static IEnumerable<Fixture> GetProvisionalFinishedFixtures(ICollection<Fixture> latestFixtures, ICollection<Fixture> current)
        {
            if(latestFixtures == null)
                return new List<Fixture>();
            
            if (current == null)
                return new List<Fixture>();

            var latestFinished = latestFixtures.Where(f => f.FinishedProvisional);
            var currentFinished = current.Where(f => f.FinishedProvisional);
            return latestFinished.Except(currentFinished);
        }
    }
}