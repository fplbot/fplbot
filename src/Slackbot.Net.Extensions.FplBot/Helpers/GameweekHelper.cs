using Fpl.Client.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class GameweekHelper : IGameweekHelper
    {
        private readonly IGameweekClient _gameweekClient;
        private readonly IMessageHelper _messageHelper;

        public GameweekHelper(
            IGameweekClient gameweekClient,
            IMessageHelper messageHelper)
        {
            _gameweekClient = gameweekClient;
            _messageHelper = messageHelper;
        }

        public async Task<int?> ExtractGameweekOrFallbackToCurrent(string messageText, string pattern)
        {
            var extractedGw = _messageHelper.ExtractGameweek(messageText, pattern);
            return extractedGw ?? (await _gameweekClient.GetGameweeks()).SingleOrDefault(x => x.IsCurrent)?.Id;
        }
    }

    internal interface IGameweekHelper
    {
        /// <summary>
        /// Extracts gameweek number from message text using pattern "some text here {gw}". E.g. "captains {gw}". Returns current gameweek if not found in text.
        /// </summary>
        /// <param name="messageText">Message text</param>
        /// <param name="pattern">Pattern (excluding bot handle). E.g. "captains {gw}"</param>
        /// <returns>Extracted gameweek if found, else current gameweek.</returns>
        Task<int?> ExtractGameweekOrFallbackToCurrent(string messageText, string pattern);
    }

}
