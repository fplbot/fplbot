using System.Text.Json;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
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
        Assert.Contains("get_injuries", names);
        Assert.Contains("get_price_changes", names);
        Assert.Contains("get_captains", names);
        Assert.Contains("get_transfers", names);
        Assert.Contains("get_gameweek", names);
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
    public async Task GetInjuries_ReturnsOnlyInjuredOverOwnershipThreshold()
    {
        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        var original = await globalSettingsClient.GetGlobalSettings();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Players =
            [
                new Player { Id = 1, WebName = "Injured Star", OwnershipPercentage = 20, ChanceOfPlayingNextRound = 25 },
                new Player { Id = 2, WebName = "Healthy Star", OwnershipPercentage = 20, ChanceOfPlayingNextRound = 100 },
                new Player { Id = 3, WebName = "Injured Nobody", OwnershipPercentage = 1, ChanceOfPlayingNextRound = 0 }
            ]
        });

        try
        {
            await using var client = await fixture.ConnectMcpClient();
            var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
            var getInjuries = tools.First(t => t.Name == "get_injuries");

            var result = await getInjuries.CallAsync(new Dictionary<string, object?>(), cancellationToken: TestContext.Current.CancellationToken);

            var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
            Assert.Contains("Injured Star", text);
            Assert.DoesNotContain("Healthy Star", text);
            Assert.DoesNotContain("Injured Nobody", text);
        }
        finally
        {
            A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(original);
        }
    }

    [Fact]
    public async Task GetPriceChanges_ReturnsAlreadyChangedAndLikelyToChange()
    {
        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        var original = await globalSettingsClient.GetGlobalSettings();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Teams = [new Team { Id = 1, Code = 100, ShortName = "ARS" }],
            Players =
            [
                new Player
                {
                    Id = 1, WebName = "Already Risen", OwnershipPercentage = 20, CostChangeEvent = 1, NowCost = 81, TeamCode = 100
                },
                new Player
                {
                    Id = 2, WebName = "About To Rise", OwnershipPercentage = 20, CostChangeEvent = 0, NowCost = 80, TeamCode = 100,
                    PriceChangeProjections = [new PriceChangeProjection { Offset = 0, Likelihood = 5, ProjectedPercent = "100" }]
                }
            ]
        });

        try
        {
            await using var client = await fixture.ConnectMcpClient();
            var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
            var getPriceChanges = tools.First(t => t.Name == "get_price_changes");

            var result = await getPriceChanges.CallAsync(new Dictionary<string, object?>(), cancellationToken: TestContext.Current.CancellationToken);

            var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
            Assert.Contains("Already Risen", text);
            Assert.Contains("About To Rise", text);
        }
        finally
        {
            A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(original);
        }
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

    [Fact]
    public async Task GetCaptains_ReturnsCaptainPicksForLeague()
    {
        const int leagueId = 700;
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        var entryClient = fixture.Services.GetRequiredService<IEntryClient>();
        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();

        // Setup league with one entry
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._, A<int?>._))
            .Returns(new ClassicLeague
            {
                Standings = new ClassicLeagueStandings
                {
                    Entries = [new ClassicLeagueEntry { Entry = 1, EntryName = "Captain FC", PlayerName = "Test Player" }]
                }
            });

        // Setup entry picks for gameweek 5
        A.CallTo(() => entryClient.GetPicks(1, 5)).Returns(new EntryPicks
        {
            ActiveChip = null,
            Picks = [new Pick { PlayerId = 10, IsCaptain = true }, new Pick { PlayerId = 20, IsViceCaptain = true }],
            EventEntryHistory = new EventEntryHistory { Bank = 0, Value = 1000 }
        });

        // Setup global settings with players and current gameweek
        var original = await globalSettingsClient.GetGlobalSettings();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Gameweeks = [new() { Id = 5, IsCurrent = true }],
            Players = [
                new Player { Id = 10, FirstName = "Erling", SecondName = "Haaland" },
                new Player { Id = 20, FirstName = "Mohamed", SecondName = "Salah" }
            ]
        });

        try
        {
            await using var client = await fixture.ConnectMcpClient();
            var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
            var getCaptains = tools.First(t => t.Name == "get_captains");

            var result = await getCaptains.CallAsync(
                new Dictionary<string, object?> { ["leagueId"] = leagueId },
                cancellationToken: TestContext.Current.CancellationToken);

            var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
            Assert.Contains("Haaland", text);
            Assert.Contains("Salah", text);
        }
        finally
        {
            A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(original);
        }
    }

    [Fact]
    public async Task GetTransfers_ReturnsTransfersForLeague()
    {
        const int leagueId = 701;
        const int gameweek = 6;
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        var transfersClient = fixture.Services.GetRequiredService<ITransfersClient>();

        // Setup league with one entry
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._, A<int?>._))
            .Returns(new ClassicLeague
            {
                Standings = new ClassicLeagueStandings
                {
                    Entries = [new ClassicLeagueEntry { Entry = 1, EntryName = "Transfer FC", PlayerName = "Test Manager" }]
                }
            });

        // Setup transfers for the entry
        A.CallTo(() => transfersClient.GetTransfers(1)).Returns(
        [
            new Transfer { Entry = 1, Event = gameweek, ElementIn = 10, ElementOut = 20 }
        ]);

        await using var client = await fixture.ConnectMcpClient();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var getTransfers = tools.First(t => t.Name == "get_transfers");

        var result = await getTransfers.CallAsync(
            new Dictionary<string, object?> { ["leagueId"] = leagueId, ["gameweek"] = gameweek },
            cancellationToken: TestContext.Current.CancellationToken);

        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        Assert.Contains("Transfer FC", text);
    }

    [Fact]
    public async Task GetGameweek_NoId_ReturnsPreviousCurrentAndNext()
    {
        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        var original = await globalSettingsClient.GetGlobalSettings();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Gameweeks =
            [
                new() { Id = 4, Name = "Gameweek 4", IsPrevious = true },
                new() { Id = 5, Name = "Gameweek 5", IsCurrent = true },
                new() { Id = 6, Name = "Gameweek 6", IsNext = true }
            ]
        });

        var fixtureClient = fixture.Services.GetRequiredService<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(4)).Returns([]);
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(5)).Returns([]);
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(6)).Returns([]);

        try
        {
            await using var client = await fixture.ConnectMcpClient();
            var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
            var getGameweek = tools.First(t => t.Name == "get_gameweek");

            var result = await getGameweek.CallAsync(new Dictionary<string, object?>(), cancellationToken: TestContext.Current.CancellationToken);

            var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
            Assert.Contains("Gameweek 4", text);
            Assert.Contains("Gameweek 5", text);
            Assert.Contains("Gameweek 6", text);
            Assert.DoesNotContain("\"requested\"", text);
        }
        finally
        {
            A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(original);
        }
    }

    [Fact]
    public async Task GetGameweek_WithId_ReturnsOnlyThatGameweek()
    {
        var globalSettingsClient = fixture.Services.GetRequiredService<IGlobalSettingsClient>();
        var original = await globalSettingsClient.GetGlobalSettings();
        A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
        {
            Gameweeks = [new() { Id = 1, Name = "Gameweek 1", IsFinished = true }]
        });

        var expectedKickOff = new DateTime(2026, 1, 15, 18, 0, 0, DateTimeKind.Utc);
        var fixtureClient = fixture.Services.GetRequiredService<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(
        [
            new Fixture
            {
                Id = 999,
                HomeTeamId = 11,
                AwayTeamId = 22,
                KickOffTime = expectedKickOff,
                Finished = true,
                HomeTeamDifficulty = 3,
                AwayTeamDifficulty = 4
            }
        ]);

        try
        {
            await using var client = await fixture.ConnectMcpClient();
            var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
            var getGameweek = tools.First(t => t.Name == "get_gameweek");

            var result = await getGameweek.CallAsync(
                new Dictionary<string, object?> { ["gameweekId"] = 1 },
                cancellationToken: TestContext.Current.CancellationToken);

            var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
            Assert.Contains("Gameweek 1", text);
            Assert.DoesNotContain("\"previous\"", text);
            Assert.DoesNotContain("\"current\"", text);
            Assert.DoesNotContain("\"next\"", text);

            var requestedFixture = JsonDocument.Parse(text).RootElement
                .GetProperty("requested")
                .GetProperty("fixtures")[0];
            Assert.Equal(999, requestedFixture.GetProperty("id").GetInt32());
            Assert.Equal(11, requestedFixture.GetProperty("homeTeamId").GetInt32());
            Assert.Equal(22, requestedFixture.GetProperty("awayTeamId").GetInt32());
            Assert.Equal(3, requestedFixture.GetProperty("homeTeamDifficulty").GetInt32());
            Assert.Equal(4, requestedFixture.GetProperty("awayTeamDifficulty").GetInt32());
            Assert.True(requestedFixture.GetProperty("finished").GetBoolean());
            Assert.Equal(expectedKickOff, requestedFixture.GetProperty("kickOffTime").GetDateTime());
        }
        finally
        {
            A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(original);
        }
    }
}
