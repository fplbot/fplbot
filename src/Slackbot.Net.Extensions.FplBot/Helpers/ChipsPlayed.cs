using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class ChipsPlayed : IChipsPlayed
    {
        private static string trippleCapChipName = "3xc";
        private static string wildcardChipName = "wildcard";
        private static string freeHitChipName = "freehit";
        private static string benchBoostChipName = "bboost";


        private readonly IEntryHistoryClient _historyClient;

        public ChipsPlayed(IEntryHistoryClient historyClient)
        {
            _historyClient = historyClient;
        }

        public async Task<bool> GetHasUsedTrippleCaptainForGameWeek(int gameweek, int teamCode)
        {
            var entryHistory = await _historyClient.GetHistory(teamCode);

            var trippleCapChip = entryHistory.Chips.FirstOrDefault(chip => chip.Name == trippleCapChipName);
            if (trippleCapChip == null)
            {
                return false;
            }
            return trippleCapChip.Event == gameweek;
        }
    }
}