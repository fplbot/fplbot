using Fpl.Client.Models;
using Fpl.Workers.Models.Mappers;

namespace FplBot.Tests;

public class PriceMonitorTests
{
    private readonly ITestOutputHelper _helper;

    public PriceMonitorTests(ITestOutputHelper helper)
    {
        _helper = helper;
    }

    [Fact]
    public void GetChangedPlayers_WhenNoPlayers_ReturnsNoChanges()
    {
        var before = new List<Player>{ };
        var after = new List<Player>{ };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team>());

        Assert.Empty(priceChanges);
    }

    [Fact]
    public void GetChangedPlayers_WhenSamePlayersWithPriceChange_ReturnsNoChanges()
    {
        var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
        var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team>());

        Assert.Empty(priceChanges);
    }

    [Fact]
    public void GetChangedPlayers_WhenSamePlayersWithChangeInPriceChange_ReturnsChanges()
    {
        var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0) };
        var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});

        Assert.Single(priceChanges);
        Assert.Equal(TestBuilder.Player().WebName, priceChanges.First().WebName);
    }

    [Fact]
    public void GetChangedPlayers_WhenSamePlayersDuplicateWithChangeInPriceChange_ReturnsSingleChanges()
    {
        var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0) };
        var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1), TestBuilder.Player().WithCostChangeEvent(1) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});

        Assert.Single(priceChanges);
        Assert.Equal(TestBuilder.Player().WebName, priceChanges.First().WebName);

        var before2 = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0), TestBuilder.Player().WithCostChangeEvent(0) };
        var after2 = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1), TestBuilder.Player().WithCostChangeEvent(1) };

        var priceChanges2 = PlayerChangesEventsExtractor.GetPriceChanges(after2,before2, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});

        Assert.Single(priceChanges2);
        Assert.Equal(TestBuilder.Player().WebName, priceChanges2.First().WebName);

        var before3 = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0), TestBuilder.Player().WithCostChangeEvent(0) };
        var after3 = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };

        var priceChanges3 = PlayerChangesEventsExtractor.GetPriceChanges(after3,before3, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});

        Assert.Single(priceChanges3);
        Assert.Equal(TestBuilder.Player().WebName, priceChanges3.First().WebName);
    }

    [Fact]
    public void GetChangedPlayers_WhenSamePlayersWithChangeInPriceRemoved_ReturnsNoChanges()
    {
        var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
        var after = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(0) };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after,before, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});

        Assert.Single(priceChanges);
    }

    [Fact]
    public void GetChangedPlayers_OneNewPlayerWithCostChange_ReturnsNewPlayer()
    {
        var before = new List<Player>{ TestBuilder.Player().WithCostChangeEvent(1) };
        var after = new List<Player>
        {
            TestBuilder.Player().WithCostChangeEvent(1),
            TestBuilder.OtherPlayer().WithCostChangeEvent(1)
        };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()});

        Assert.Empty(priceChanges);
    }

    [Fact]
    public void GetChangedPlayers_OnePlayerRemoved_ReturnsNoChanges()
    {
        var before = new List<Player>
        {
            TestBuilder.Player().WithCostChangeEvent(1),
            TestBuilder.OtherPlayer().WithCostChangeEvent(1)
        };

        var after = new List<Player>
        {
            TestBuilder.Player().WithCostChangeEvent(1)
        };

        var priceChanges = PlayerChangesEventsExtractor.GetPriceChanges(after, before, new List<Team>());

        Assert.Empty(priceChanges);
    }
}
