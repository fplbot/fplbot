using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace FplBot.Tests.E2E.ApiEndpoints;

[Collection("App")]
public class McpEndpointsTests(AppFixture fixture)
{
    [Fact]
    public async Task ListTools_ExposesAllFplTools()
    {
        await using var client = await fixture.ConnectMcpClient();

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var names = tools.Select(t => t.Name).ToList();
        Assert.Contains("get_league", names);
        Assert.Contains("get_league_details", names);
        Assert.Contains("get_entry", names);
        Assert.Contains("search_entries", names);
        Assert.Contains("search_leagues", names);
        Assert.Contains("search_any", names);
        Assert.Contains("get_player", names);
    }

    [Fact]
    public async Task ListTools_OnMcpFplbotAppHost_AlsoWorksAtRoot()
    {
        await using var client = await fixture.ConnectMcpClient("http://mcp.fplbot.app/");

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("get_league", tools.Select(t => t.Name));
    }

    [Fact]
    public async Task GetLeague_Found_ReturnsLeagueNameAndAdmin()
    {
        const int leagueId = 600;
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns(new ClassicLeague
        {
            Properties = new ClassicLeagueProperties { Name = "MCP League", AdminEntry = 1 },
            Standings = new ClassicLeagueStandings
            {
                Entries = [new ClassicLeagueEntry { Entry = 1, PlayerName = "MCP Admin" }]
            }
        });

        await using var client = await fixture.ConnectMcpClient();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var getLeague = tools.First(t => t.Name == "get_league");

        var result = await getLeague.CallAsync(
            new Dictionary<string, object?> { ["leagueId"] = leagueId },
            cancellationToken: TestContext.Current.CancellationToken);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("MCP League", text);
        Assert.Contains("MCP Admin", text);
    }

    [Fact]
    public async Task GetLeague_NotFound_ReturnsNullResult()
    {
        const int leagueId = 601;
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns((ClassicLeague?)null);

        await using var client = await fixture.ConnectMcpClient();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var getLeague = tools.First(t => t.Name == "get_league");

        var result = await getLeague.CallAsync(
            new Dictionary<string, object?> { ["leagueId"] = leagueId },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(result.Content);
    }

    [Fact]
    public async Task GetPlayer_FuzzyMatch_ReturnsPlayer()
    {
        await using var client = await fixture.ConnectMcpClient();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var getPlayer = tools.First(t => t.Name == "get_player");

        var result = await getPlayer.CallAsync(
            new Dictionary<string, object?> { ["name"] = "van dijk" },
            cancellationToken: TestContext.Current.CancellationToken);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("Virgil van Dijk", text);
    }

    [Fact]
    public async Task GetEntry_Found_ReturnsEntry()
    {
        var searchService = fixture.Services.GetRequiredService<ISearchService>();
        A.CallTo(() => searchService.GetEntry(42)).Returns(new EntryItem { Id = 42, RealName = "Magnus Skjelbek" });

        await using var client = await fixture.ConnectMcpClient();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var getEntry = tools.First(t => t.Name == "get_entry");

        var result = await getEntry.CallAsync(
            new Dictionary<string, object?> { ["id"] = 42 },
            cancellationToken: TestContext.Current.CancellationToken);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("Magnus Skjelbek", text);
    }

    [Fact]
    public async Task SearchEntries_ReturnsMatchingHits()
    {
        var searchService = fixture.Services.GetRequiredService<ISearchService>();
        A.CallTo(() => searchService.SearchForEntry("skjelbek", 0, 10, A<SearchMetaData>.That.Matches(m => m.Client == QueryClient.Mcp)))
            .Returns(new SearchResult<EntryItem>([new EntryItem { Id = 1, RealName = "Magnus Skjelbek" }], totalHits: 1, page: 0, maxHits: 10));

        await using var client = await fixture.ConnectMcpClient();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var searchEntries = tools.First(t => t.Name == "search_entries");

        var result = await searchEntries.CallAsync(
            new Dictionary<string, object?> { ["query"] = "skjelbek", ["page"] = 0 },
            cancellationToken: TestContext.Current.CancellationToken);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("Magnus Skjelbek", text);
    }

    [Fact]
    public async Task SearchLeagues_ReturnsMatchingHits()
    {
        var searchService = fixture.Services.GetRequiredService<ISearchService>();
        A.CallTo(() => searchService.SearchForLeague("gaffers", 0, 10, A<SearchMetaData>.That.Matches(m => m.Client == QueryClient.Mcp), null))
            .Returns(new SearchResult<LeagueItem>([new LeagueItem { Id = 1, Name = "The Gaffers League" }], totalHits: 1, page: 0, maxHits: 10));

        await using var client = await fixture.ConnectMcpClient();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var searchLeagues = tools.First(t => t.Name == "search_leagues");

        var result = await searchLeagues.CallAsync(
            new Dictionary<string, object?> { ["query"] = "gaffers", ["page"] = 0 },
            cancellationToken: TestContext.Current.CancellationToken);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("The Gaffers League", text);
    }

    [Fact]
    public async Task SearchAny_ReturnsMatchingHits()
    {
        var searchService = fixture.Services.GetRequiredService<ISearchService>();
        A.CallTo(() => searchService.SearchAny("skjelbek", 0, 10, A<SearchMetaData>.That.Matches(m => m.Client == QueryClient.Mcp), SearchType.All))
            .Returns(new SearchResult<dynamic>([new EntryItem { Id = 1, RealName = "Magnus Skjelbek" }], totalHits: 1, page: 0, maxHits: 10));

        await using var client = await fixture.ConnectMcpClient();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var searchAny = tools.First(t => t.Name == "search_any");

        var result = await searchAny.CallAsync(
            new Dictionary<string, object?> { ["query"] = "skjelbek", ["page"] = 0 },
            cancellationToken: TestContext.Current.CancellationToken);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("Magnus Skjelbek", text);
    }
}
