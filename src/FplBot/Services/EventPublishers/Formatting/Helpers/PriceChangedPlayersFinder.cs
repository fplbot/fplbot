using Fpl.Client.Models;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Services.WebApi.Slack.Extensions;

namespace FplBot.Formatting.Helpers;

public class PriceChangedPlayersFinder : IPriceChangedPlayersFinder
{
    public IEnumerable<PlayerWithPriceChange> FindPriceChangedPlayers(ICollection<Player> players, ICollection<Team> teams)
    {
        return players.Where(p => p.CostChangeEvent != 0 && p.IsRelevant())
            .Select(p =>
            {
                var t = teams.First(t => t.Code == p.TeamCode);
                return new PlayerWithPriceChange(p.Id, p.WebName ?? "", p.CostChangeEvent, p.NowCost, p.OwnershipPercentage, t.Id, t.ShortName ?? "");
            });
    }
}
