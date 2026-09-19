using Fpl.Search.Models;

namespace FplBot.Tests.E2E.Search;

[Collection("App")]
public class SearchQueryAnalyticsTests(AppFixture fixture) : IAsyncLifetime
{
    public ValueTask InitializeAsync()
    {
        fixture.IndexedQueries.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ASearchRecordsTheCallersIpAgainstTheQuery()
    {
        var consumedBefore = fixture.ConsumedSoFar;

        var response = await fixture.GetFrom("/api/search/entries?query=messi&page=0", "203.0.113.5");

        response.EnsureSuccessStatusCode();
        await fixture.WaitUntilBusIdle(consumedBefore);

        var indexed = Assert.Single(fixture.IndexedQueries.Queries);
        Assert.Equal("messi", indexed.Query);
        Assert.Equal("203.0.113.5", indexed.Actor);
        Assert.Equal(nameof(QueryClient.Web), indexed.Client);
    }
}
