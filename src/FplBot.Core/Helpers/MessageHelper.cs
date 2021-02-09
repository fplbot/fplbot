using System.Text.RegularExpressions;

namespace FplBot.Core.Helpers
{
    public static class MessageHelper
    {
        /// <summary>
        /// Extracts gameweek number from message text using pattern "some text here {gw}". E.g. "captains {gw}".
        /// </summary>
        /// <param name="messageText">Message text</param>
        /// <param name="pattern">Pattern (excluding bot handle). E.g. "captains {gw}"</param>
        /// <returns>Extracted gameweek if found</returns>
        public static int? ExtractGameweek(string messageText, string pattern)
        {
            var gameweek = FindMatch(messageText, $"{pattern.Replace("{gw}", "(\\d+?)(?:\\s|$)")}");
            return gameweek == null ? (int?) null : int.Parse(gameweek);
        }
        
        /// <summary>
        /// Extracts arguments from message text using pattern "some text here {args}". E.g. "player {args}".
        /// </summary>
        /// <param name="messageText">Message text</param>
        /// <param name="pattern">Pattern (excluding bot handle). E.g. "player {args}"</param>
        /// <returns>Extracted arguments if found</returns>
        public static string ExtractArgs(string messageText, string pattern)
        {
            return FindMatch(messageText, $"{pattern.Replace("{args}", "(.+)?")}");
        }
        
        /// <summary>
        /// Extracts arguments from message text using pattern "some text here {args}". E.g. "player {args}".
        /// </summary>
        /// <param name="messageText">Message text</param>
        /// <param name="patterns">Patterns (excluding bot handle). E.g. ["subscribe {args}", "unsubscribe {args}"]</param>
        /// <returns>Extracted arguments if found</returns>
        public static string ExtractArgs(string messageText, string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                var match = FindMatch(messageText, $"{pattern.Replace("{args}", "(.+)?")}");
                if (match != null)
                    return match;
            }

            return null;
        }

        private static string FindMatch(string input, string regexPattern)
        {
            var regex = new Regex(regexPattern);
            var result = regex.Match(input).Groups;
            return result.Count > 1 ? result[1].Value : null;
        }
    }
}
