using Fpl.Client.Models;

namespace FplBot.Formatting;

public class FinishedFixture
{
    public Fixture Fixture { get; set; } = null!;
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;

    public IEnumerable<BonusPointsPlayer> BonusPoints { get; set; } = new List<BonusPointsPlayer>();
    public IEnumerable<DefensiveContributionPlayer> DefensiveContributions { get; set; } = new List<DefensiveContributionPlayer>();
}
