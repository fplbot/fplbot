using FplBot.Formatting.FixtureStats.Formatters;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats;

public class GameweekEventsFormatter
{
    public static List<FixtureEventMessage> FormatNewFixtureEvents(List<FixtureEvents> newFixtureEvents, Func<StatType,bool> subscribesToStat, FormattingType formattingType, TauntData tauntData = null)
    {
        var formattedStrings = new List<FixtureEventMessage>();
        var statFormatterFactory = new StatFormatterFactory(tauntData, formattingType);

        newFixtureEvents.ForEach(newFixtureEvent =>
        {
            var eventMessages = newFixtureEvent.StatMap
                .Where(stat => subscribesToStat(stat.Key))
                .SelectMany(stat => statFormatterFactory.Create(stat.Key).Format(stat.Value))
                .WhereNotNull()
                .MaterializeToArray();
            if (eventMessages.Any())
            {
                formattedStrings.Add(new($"{GetScore(newFixtureEvent)}", Formatter.BulletPoints(eventMessages)));
            }
        });

        return formattedStrings;
    }

    private static string GetScore(FixtureEvents fixtureEvent)
    {
        var gameTime =fixtureEvent.FixtureScore.Minutes != 0 ?  $"({fixtureEvent.FixtureScore.Minutes}')" : "";
        return $"{fixtureEvent.FixtureScore.HomeTeam.Name} " +
               $"{fixtureEvent.FixtureScore.HomeTeamScore}-{fixtureEvent.FixtureScore.AwayTeamScore} " +
               $"{fixtureEvent.FixtureScore.AwayTeam.Name} " +
               gameTime;
    }
}
