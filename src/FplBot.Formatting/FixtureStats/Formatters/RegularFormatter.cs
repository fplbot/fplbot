using FplBot.Formatting.FixtureStats.Describers;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Formatting.FixtureStats.Formatters;

internal class RegularFormatter : IFormat
{
    private readonly IDescribeEvents _describer;
    private readonly FormattingType _formattingType;

    public RegularFormatter(IDescribeEvents describer, FormattingType formattingType)
    {
        _describer = describer;
        _formattingType = formattingType;
    }

    public IEnumerable<string> Format(IEnumerable<PlayerEvent> events)
    {
        return events.GroupBy(g => g.Player).Select( g =>
        {
            var message = string.Format(_describer.EventDescriptionSingular, $"{g.Key.FirstName} {g.Key.SecondName}", _describer.EventEmoji);
            if (g.Count() > 1)
            {
                var multipleEmojis = String.Concat(Enumerable.Repeat(_describer.EventEmoji, g.Count()));
                message = string.Format(_describer.EventDescriptionPlural, $"{g.Key.FirstName} {g.Key.SecondName} {multipleEmojis}", g.Count(), _describer.EventEmoji);
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
        switch (_formattingType)
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