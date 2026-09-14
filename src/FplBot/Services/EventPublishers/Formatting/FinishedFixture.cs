using Fpl.Client.Models;

namespace FplBot.Formatting;

public class FinishedFixture
{
    public Fixture Fixture { get; set; } = null!;
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;

    public IEnumerable<BonusPointsPlayer> BonusPoints { get; set; } = [];
    public IEnumerable<DefensiveContributionPlayer> DefensiveContributions { get; set; } = [];
    public IEnumerable<TopPerformer> TopPerformers { get; set; } = [];
}
