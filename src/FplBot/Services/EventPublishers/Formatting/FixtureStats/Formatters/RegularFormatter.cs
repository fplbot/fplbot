using FplBot.Formatting.FixtureStats.Describers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats.Formatters;

internal class RegularFormatter(IDescribeEvents describer, FormattingType formattingType) : IFormat
{
    public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
    {
        return events.GroupBy(g => g.Player).Select( g =>
        {
            var message = string.Format(describer.EventDescriptionSingular, $"{g.Key.WebName}", describer.EventEmoji);
            if (g.Count() > 1)
            {
                var multipleEmojis = String.Concat(Enumerable.Repeat(describer.EventEmoji, g.Count()));
                message = string.Format(describer.EventDescriptionPlural, $"{g.Key.WebName} {multipleEmojis}", g.Count(), describer.EventEmoji);
            }

            if (g.Any(g => g.IsRemoved))
            {
                message = $"{StrikeThrough()}{message.TrimEnd()}{StrikeThrough()} (VAR? 🤷‍♀️)";
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
                return "ℹ️";
        }
    }
}
