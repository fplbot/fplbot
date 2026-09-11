using FplBot.Formatting.FixtureStats.Describers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats.Formatters;

internal class TauntyFormatter(IDescribeTaunts describer, TauntData tauntData, FormattingType formattingType)
    : IFormat
{
    public IEnumerable<string> Format(IEnumerable<PlayerEvent> goalEvents)
    {
        return goalEvents.GroupBy(g => g.Player).Select(g =>
        {
            var message = string.Format(describer.EventDescriptionSingular, $"{g.Key.WebName}", describer.EventEmoji);
            if (g.Count() > 1)
            {
                var multipleEmojis = String.Concat(Enumerable.Repeat(describer.EventEmoji, g.Count()));
                message = string.Format(describer.EventDescriptionPlural, $"{g.Key.WebName}", g.Count(), multipleEmojis);
            }
            if (g.Any(g => g.IsRemoved))
            {
                message = $"{StrikeThrough()}{message.TrimEnd()}{StrikeThrough()} (VAR? 🤷‍♀️)";
            }
            else
            {
                var tauntibleEntries = tauntData.GetTauntibleEntries(g.Key, describer.Type);
                var jokeFormat = describer.JokePool.GetRandom();
                var append = tauntibleEntries.Any() && jokeFormat != null ? $" {string.Format(jokeFormat, string.Join(", ", tauntibleEntries))}" : null;
                message += append;
            }

            return message;

        });
    }

    private string StrikeThrough()
    {
        switch (formattingType)
        {
            case FormattingType.Slack:
                return "~";
            case FormattingType.Discord:
                return "~~";
            default:
                return "ð";
        }
    }
}
