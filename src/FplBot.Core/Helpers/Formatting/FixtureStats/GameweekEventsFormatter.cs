using System.Collections.Generic;
using System.Linq;
using FplBot.Core.Abstractions;
using FplBot.Core.Data.Models;
using FplBot.Core.Extensions;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Core.Helpers
{
    internal class GameweekEventsFormatter
    {
        public static List<string> FormatNewFixtureEvents(List<FixtureEvents> newFixtureEvents, IEnumerable<EventSubscription> subscriptions, TauntData tauntData)
        {
            var formattedStrings = new List<string>();
            var statFormatterFactory = new StatFormatterFactory(tauntData);

            newFixtureEvents.ForEach(newFixtureEvent =>
            {
                var eventMessages = newFixtureEvent.StatMap
                    .Where(stat => subscriptions.ContainsStat(stat.Key))
                    .SelectMany(stat => statFormatterFactory.Create(stat.Key).Format(stat.Value))
                    .WhereNotNull()
                    .MaterializeToArray();
                if (eventMessages.Any())
                {
                    formattedStrings.Add($"{GetScore(newFixtureEvent)}\n" + Formatter.BulletPoints(eventMessages));
                }
            });

            return formattedStrings;
        }

        private static string GetScore(FixtureEvents fixtureEvent)
        {
            return $"{fixtureEvent.FixtureScore.HomeTeam.Name} " +
                   $"{fixtureEvent.FixtureScore.HomeTeamScore}-{fixtureEvent.FixtureScore.AwayTeamScore} " +
                   $"{fixtureEvent.FixtureScore.AwayTeam.Name}";
        }
    }
}
