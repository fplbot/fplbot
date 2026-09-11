using Fpl.Client;
using Fpl.Client.Models;

namespace FplBot.Formatting.Helpers;

public static class FixtureFulltimeModelBuilder
{
    // Number of players listed under "Top performers". Players tied with the last one are kept as well.
    public const int TopPerformersCount = 5;

    public static FinishedFixture CreateFinishedFixture(ICollection<Team> teams, ICollection<Player> players, Fixture n, ICollection<LiveItem>? liveItems = null)
    {
        return new FinishedFixture
        {
            Fixture = n,
            HomeTeam = teams.First(t => t.Id == n.HomeTeamId),
            AwayTeam = teams.First(t => t.Id == n.AwayTeamId),
            BonusPoints = CreateBonusPlayers(players, n),
            DefensiveContributions = CreateDefensiveContributionPlayers(players, n),
            TopPerformers = CreateTopPerformers(players, n, liveItems)
        };
    }

    // The live endpoint (event/{gw}/live) reports, per player, an "explain" breakdown with the points earned
    // in each fixture of the gameweek. Summing the entry for this fixture gives the player's points for this
    // match alone, which keeps double gameweeks correct. The points are provisional at this stage, just like
    // the bonus points and defensive contributions.
    private static IEnumerable<TopPerformer> CreateTopPerformers(ICollection<Player> players, Fixture fixture, ICollection<LiveItem>? liveItems)
    {
        try
        {
            if (liveItems == null || liveItems.Count == 0)
                return new List<TopPerformer>();

            var ranked = liveItems
                .Select(ToTopPerformer)
                .Where(tp => tp != null && tp.Points > 0)
                .Select(tp => tp!)
                .OrderByDescending(tp => tp.Points)
                .ThenBy(tp => tp.Player.WebName)
                .ToList();

            if (ranked.Count <= TopPerformersCount)
                return ranked;

            var cutoff = ranked[TopPerformersCount - 1].Points;
            return ranked.Where(tp => tp.Points >= cutoff).ToList();

            TopPerformer? ToTopPerformer(LiveItem item)
            {
                var explain = item.Explain.FirstOrDefault(e => e.Fixture == fixture.Id);
                if (explain == null)
                    return null;

                var player = players.FirstOrDefault(p => p.Id == item.Id);
                if (player == null)
                    return null;

                if (player.TeamId != fixture.HomeTeamId && player.TeamId != fixture.AwayTeamId)
                    return null;

                return new TopPerformer
                {
                    Player = player,
                    Points = explain.Stats.Sum(s => s.Points)
                };
            }
        }
        catch
        {
            return new List<TopPerformer>();
        }
    }

    private static IEnumerable<BonusPointsPlayer> CreateBonusPlayers(ICollection<Player> players, Fixture fixture)
    {
        try
        {
            var bonusPointsHome = fixture.Stats.FirstOrDefault(s => s.Identifier == FplConstants.StatIdentifiers.Bps)?.HomeStats;
            var bonusPointsAway = fixture.Stats.FirstOrDefault(s => s.Identifier == FplConstants.StatIdentifiers.Bps)?.AwayStats;

            var home = (bonusPointsHome ?? Enumerable.Empty<FixtureStatValue>()).Select(BpsFilter).ToList();
            var away = (bonusPointsAway ?? Enumerable.Empty<FixtureStatValue>()).Select(BpsFilter).ToList();
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

    // The FPL fixtures endpoint lists, under "defensive_contribution", every player who made at least one
    // defensive contribution in the match together with their count. Only players reaching the threshold
    // for their position are awarded points, so only those are kept here.
    private static IEnumerable<DefensiveContributionPlayer> CreateDefensiveContributionPlayers(ICollection<Player> players, Fixture fixture)
    {
        try
        {
            var stat = fixture.Stats.FirstOrDefault(s => s.Identifier == FplConstants.StatIdentifiers.DefensiveContribution);
            if (stat == null)
                return new List<DefensiveContributionPlayer>();

            var home = stat.HomeStats ?? new List<FixtureStatValue>();
            var away = stat.AwayStats ?? new List<FixtureStatValue>();

            return home.Concat(away)
                .Select(ToDefensiveContributionPlayer)
                .Where(dc => dc != null && ReachedThreshold(dc!))
                .Select(dc => dc!)
                .OrderByDescending(dc => dc.Contributions)
                .ThenBy(dc => dc.Player.WebName)
                .ToList();

            DefensiveContributionPlayer? ToDefensiveContributionPlayer(FixtureStatValue value)
            {
                var player = players.FirstOrDefault(p => p.Id == value.Element);
                if (player == null)
                    return null;

                return new DefensiveContributionPlayer
                {
                    Player = player,
                    Contributions = value.Value
                };
            }
        }
        catch
        {
            return new List<DefensiveContributionPlayer>();
        }
    }

    public static bool ReachedThreshold(DefensiveContributionPlayer dc)
    {
        return dc.Player.Position switch
        {
            FplPlayerPosition.Defender => dc.Contributions >= FplConstants.DefensiveContributionThresholds.Defender,
            FplPlayerPosition.Midfielder or FplPlayerPosition.Forward => dc.Contributions >= FplConstants.DefensiveContributionThresholds.MidfielderAndForward,
            _ => false
        };
    }
}
