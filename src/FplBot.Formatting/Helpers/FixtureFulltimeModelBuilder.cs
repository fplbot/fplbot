using System.Collections.Generic;
using System.Linq;
using Fpl.Client.Models;

namespace FplBot.Formatting.Helpers;

public static class FixtureFulltimeModelBuilder
{
    public static FinishedFixture CreateFinishedFixture(ICollection<Team> teams, ICollection<Player> players, Fixture n)
    {
        return new FinishedFixture
        {
            Fixture = n,
            HomeTeam = teams.First(t => t.Id == n.HomeTeamId),
            AwayTeam = teams.First(t => t.Id == n.AwayTeamId),
            BonusPoints = CreateBonusPlayers(players, n)
        };
    }

    private static IEnumerable<BonusPointsPlayer> CreateBonusPlayers(ICollection<Player> players, Fixture fixture)
    {
        try
        {
            var bonusPointsHome = fixture.Stats.FirstOrDefault(s => s.Identifier == "bps")?.HomeStats;
            var bonusPointsAway = fixture.Stats.FirstOrDefault(s => s.Identifier == "bps")?.AwayStats;

            var home = bonusPointsHome.Select(BpsFilter).ToList();
            var away = bonusPointsAway.Select(BpsFilter).ToList();
            var aggregated = home.Concat(away).OrderByDescending(bpp => bpp.BonusPoints);
            return aggregated;

            BonusPointsPlayer BpsFilter(FixtureStatValue bps)
            {
                return new BonusPointsPlayer
                {
                    Player = players.First(p => p.Id == bps.Element),
                    BonusPoints = bps.Value
                };
            }
        }
        catch
        {
            return new List<BonusPointsPlayer>();
        }
    }
}