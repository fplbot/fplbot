using Slackbot.Net.Abstractions.Handlers;
using System.Text.RegularExpressions;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class MessageHelper : IMessageHelper
    {
        private readonly BotDetails _botDetails;

        public MessageHelper(BotDetails botDetails)
        {
            _botDetails = botDetails;
        }

        public int? ExtractGameweek(string messageText, string pattern)
        {
            var gameweek = FindMatch(messageText, $"{BotPattern} {pattern.Replace("{gw}", "(\\d+?)(?:\\s|$)")}");
            return gameweek == null ? (int?) null : int.Parse(gameweek);
        }
        
        public string ExtractArgs(string messageText, string pattern)
        {
            return FindMatch(messageText, $"{BotPattern} {pattern.Replace("{args}", "(.+?)$")}");
        }

        private static string FindMatch(string input, string regexPattern)
        {
            var regex = new Regex(regexPattern);
            var result = regex.Match(input).Groups;
            return result.Count > 1 ? result[1].Value : null;
        }

        private string BotPattern => $"(?:@{_botDetails.Name}|<@{_botDetails.Id}>)";
    }

    internal interface IMessageHelper
    {
        /// <summary>
        /// Extracts gameweek number from message text using pattern "some text here {gw}". E.g. "captains {gw}".
        /// </summary>
        /// <param name="messageText">Message text</param>
        /// <param name="pattern">Pattern (excluding bot handle). E.g. "captains {gw}"</param>
        /// <returns>Extracted gameweek if found</returns>
        int? ExtractGameweek(string messageText, string pattern);

        /// <summary>
        /// Extracts arguments from message text using pattern "some text here {args}". E.g. "player {args}".
        /// </summary>
        /// <param name="messageText">Message text</param>
        /// <param name="pattern">Pattern (excluding bot handle). E.g. "player {args}"</param>
        /// <returns>Extracted arguments if found</returns>
        string ExtractArgs(string messageText, string pattern);
    }
}
