using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.UnitTests.Publishers;

public class PlayerChangesEventsExtractorTests(ITestOutputHelper helper)
{
    private readonly ITestOutputHelper _helper = helper;

    [Fact]
    public void GetChangedPlayers_WhenNoPlayers_ReturnsNoChanges()
    {
        var before = new List<Player>();
        var after = new List<Player>();

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, []);

        Assert.Empty(priceChanges);
    }

    [Fact]
    public void GetChangedPlayers_WhenSamePlayersWithPriceChange_ReturnsNoChanges()
    {
        var before = new List<Player> { TestBuilder.Player().WithCost(1) };
        var after = new List<Player> { TestBuilder.Player().WithCost(1) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, []);

        Assert.Empty(priceChanges);
    }

    [Fact]
    public void GetChangedPlayers_WhenSamePlayersWithChangeInPriceChange_ReturnsChanges()
    {
        var before = new List<Player> { TestBuilder.Player().WithCost(0) };
        var after = new List<Player> { TestBuilder.Player().WithCost(1) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]);

        Assert.Single(priceChanges);
        Assert.Equal(TestBuilder.Player().WebName, priceChanges.First().WebName);
    }

    [Fact]
    public void GetChangedPlayers_WhenSamePlayersDuplicateWithChangeInPriceChange_ReturnsSingleChanges()
    {
        var before = new List<Player> { TestBuilder.Player().WithCost(0) };
        var after = new List<Player> { TestBuilder.Player().WithCost(1), TestBuilder.Player().WithCost(1) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]);

        Assert.Single(priceChanges);
        Assert.Equal(TestBuilder.Player().WebName, priceChanges.First().WebName);

        var before2 = new List<Player> { TestBuilder.Player().WithCost(0), TestBuilder.Player().WithCost(0) };
        var after2 = new List<Player> { TestBuilder.Player().WithCost(1), TestBuilder.Player().WithCost(1) };

        var priceChanges2 = PlayerChangesEventsExtractor.GetPriceChanges(after2, before2, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]);

        Assert.Single(priceChanges2);
        Assert.Equal(TestBuilder.Player().WebName, priceChanges2.First().WebName);

        var before3 = new List<Player> { TestBuilder.Player().WithCost(0), TestBuilder.Player().WithCost(0) };
        var after3 = new List<Player> { TestBuilder.Player().WithCost(1) };

        var priceChanges3 = PlayerChangesEventsExtractor.GetPriceChanges(after3, before3, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]);

        Assert.Single(priceChanges3);
        Assert.Equal(TestBuilder.Player().WebName, priceChanges3.First().WebName);
    }

    [Fact]
    public void GetChangedPlayers_WhenSamePlayersWithChangeInPriceRemoved_ReturnsNoChanges()
    {
        var before = new List<Player> { TestBuilder.Player().WithCost(1) };
        var after = new List<Player> { TestBuilder.Player().WithCost(0) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]);

        Assert.Single(priceChanges);
    }

    [Fact]
    public void GetChangedPlayers_OneNewPlayerWithCostChange_ReturnsNewPlayer()
    {
        var before = new List<Player> { TestBuilder.Player().WithCost(1) };
        var after = new List<Player> { TestBuilder.Player().WithCost(1), TestBuilder.OtherPlayer().WithCost(1) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]);

        Assert.Empty(priceChanges);
    }

    [Fact]
    public void GetChangedPlayers_OnePlayerRemoved_ReturnsNoChanges()
    {
        var before = new List<Player> { TestBuilder.Player().WithCost(1), TestBuilder.OtherPlayer().WithCost(1) };

        var after = new List<Player> { TestBuilder.Player().WithCost(1) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, []);

        Assert.Empty(priceChanges);
    }

    [Fact]
    public void GetLikelyPriceChanges_WhenNoProjection_ReturnsNoChanges()
    {
        var after = new List<Player> { TestBuilder.Player() };

        var likely = PlayerChangesEventsExtractor.GetLikelyPriceChanges(after, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]);

        Assert.Empty(likely);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-4)]
    [InlineData(0)]
    public void GetLikelyPriceChanges_WhenBelowVeryLikelyThreshold_ReturnsNoChanges(int likelihood)
    {
        var after = new List<Player> { TestBuilder.Player().WithNextPriceChangeLikelihood(likelihood) };

        var likely = PlayerChangesEventsExtractor.GetLikelyPriceChanges(after, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]);

        Assert.Empty(likely);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-5)]
    public void GetLikelyPriceChanges_WhenVeryLikelyThreshold_ReturnsThePlayer(int likelihood)
    {
        var after = new List<Player> { TestBuilder.Player().WithNextPriceChangeLikelihood(likelihood, "123.4") };

        var likely = PlayerChangesEventsExtractor.GetLikelyPriceChanges(after, [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()]).ToList();

        Assert.Single(likely);
        Assert.Equal(TestBuilder.Player().WebName, likely[0].WebName);
        Assert.Equal(likelihood, likely[0].Likelihood);
        Assert.Equal("123.4", likely[0].ProjectedPercent);
        Assert.Equal(TestBuilder.HomeTeam().Id, likely[0].TeamId);
    }
}
