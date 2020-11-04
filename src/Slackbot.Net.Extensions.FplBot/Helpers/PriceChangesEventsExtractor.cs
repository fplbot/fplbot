using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;
using Slackbot.Net.Extensions.FplBot.Models;
using Slackbot.Net.Extensions.FplBot.PriceMonitoring;

namespace Slackbot.Net.Extensions.FplBot.Helpers
{
    internal class PriceChangesEventsExtractor
    {
        public static IEnumerable<PriceChange> GetPriceChanges(ICollection<Player> after, ICollection<Player> players, ICollection<Team> teams)
        {
            var playersWithPriceChanges = after.Where(p => p.CostChangeEvent != 0).ToList();
            var playersWithNewPrices = playersWithPriceChanges.Except(players, new PlayerPriceComparer()).ToList();
            return playersWithNewPrices.Select(p => new PriceChange
            {
                PlayerWebName = p.WebName,
                PlayerFirstName = p.FirstName,
                PlayerSecondName = p.SecondName,
                NowCost = p.NowCost,
                CostChangeEvent = p.CostChangeEvent,
                TeamName = teams.FirstOrDefault(t => t.Code == p.TeamCode)?.Name
            });
        }
    }
}