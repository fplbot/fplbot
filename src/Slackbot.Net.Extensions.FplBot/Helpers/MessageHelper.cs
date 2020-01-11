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
            var regex = new Regex($"(?:@{Constants.FplBotName}|<@{_botDetails.Id}>) {pattern.Replace("{gw}", "(\\d+?)(?:\\s|$)")}");
            var result = regex.Match(messageText).Groups;
            return result.Count > 1 ? int.Parse(result[1].Value) : (int?)null;
        }
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
    }
}
