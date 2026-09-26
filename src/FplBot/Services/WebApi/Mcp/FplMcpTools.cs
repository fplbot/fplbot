using System.ComponentModel;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.WebApi.Endpoints.Api.Fpl;
using ModelContextProtocol.Server;

namespace FplBot.WebApi.Mcp;

[McpServerToolType]
public class FplMcpTools(
    ILeagueClient leagueClient,
    IEntryClient entryClient,
    ITransfersClient transfersClient,
    IEntryHistoryClient entryHistoryClient,
    IGlobalSettingsClient globalSettingsClient,
    IPlayerSearch playerSearch,
    IInjuredPlayersFinder injuredPlayersFinder,
    IPriceChangedPlayersFinder priceChangedPlayersFinder,
    ISearchService searchService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<Program> logger)
{
    private const int MaxHits = 10;

    [McpServerTool(Name = "get_league", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Look up a classic FPL league by id: its name and admin manager's name.")]
    public Task<object?> GetLeague(
        [Description("The classic league's FPL id")] int leagueId) =>
        FplEndpoints.GetLeagueData(leagueId, leagueClient, logger);

    [McpServerTool(Name = "get_league_details", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Full classic league standings plus current-gameweek summaries (captain, vice-captain, chip, transfers) per entry.")]
    public Task<object?> GetLeagueDetails(
        [Description("The classic league's FPL id")] int leagueId) =>
        FplEndpoints.GetLeagueDetailsData(leagueId, leagueClient, entryClient, transfersClient, entryHistoryClient, globalSettingsClient, logger);

    [McpServerTool(Name = "get_player", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Look up an FPL player by name (fuzzy match — handles nicknames, misspellings, and partial names). Returns full player stats.")]
    public async Task<Player?> GetPlayer(
        [Description("Player name, nickname, or partial name (e.g. 'haaland', 'van dijk', 'kun')")] string name)
    {
        var settings = await globalSettingsClient.GetGlobalSettings();
        return playerSearch.FindMostPopularMatchingPlayer(settings?.Players ?? [], name);
    }

    [McpServerTool(Name = "get_injuries", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("List currently injured or doubtful players with over 5% ownership, ranked by ownership.")]
    public async Task<IEnumerable<InjuredPlayerSummary>> GetInjuries()
    {
        var settings = await globalSettingsClient.GetGlobalSettings();
        var injured = injuredPlayersFinder.FindInjuredPlayers(settings?.Players ?? []);
        return injured.Select(p => new InjuredPlayerSummary(
            p.Id, p.WebName ?? "", p.Status, p.News, p.ChanceOfPlayingNextRound, p.OwnershipPercentage, p.TeamId));
    }

    [McpServerTool(Name = "get_price_changes", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Players (over 7% ownership) whose price already changed today, plus players very likely to change price soon.")]
    public async Task<PriceChangesResponse> GetPriceChanges()
    {
        var settings = await globalSettingsClient.GetGlobalSettings();
        var players = settings?.Players ?? [];
        var teams = settings?.Teams ?? [];

        var alreadyChanged = priceChangedPlayersFinder.FindPriceChangedPlayers(players, teams);
        var likelyToChange = PlayerChangesEventsExtractor.GetLikelyPriceChanges(players, teams);

        return new PriceChangesResponse(alreadyChanged, likelyToChange);
    }

    [McpServerTool(Name = "get_entry", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Look up a single FPL manager entry by id.")]
    public Task<EntryItem?> GetEntry(
        [Description("The FPL manager entry id")] int id) =>
        searchService.GetEntry(id);

    [McpServerTool(Name = "search_entries", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Search for FPL manager entries by manager or team name.")]
    public Task<SearchResult<EntryItem>> SearchEntries(
        [Description("Free-text search query")] string query,
        [Description("Zero-based page number")] int page = 0) =>
        searchService.SearchForEntry(query, page, MaxHits, BuildMetaData());

    [McpServerTool(Name = "search_leagues", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Search for classic FPL leagues by name.")]
    public Task<SearchResult<LeagueItem>> SearchLeagues(
        [Description("Free-text search query")] string query,
        [Description("Zero-based page number")] int page = 0,
        [Description("Optional country name to boost in ranking")] string? countryToBoost = null) =>
        searchService.SearchForLeague(query, page, MaxHits, BuildMetaData(), countryToBoost);

    [McpServerTool(Name = "search_any", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Search both FPL manager entries and leagues in one query.")]
    public Task<SearchResult<dynamic>> SearchAny(
        [Description("Free-text search query")] string query,
        [Description("Zero-based page number")] int page = 0,
        [Description("Restrict to entries, leagues, or both")] SearchType type = SearchType.All) =>
        searchService.SearchAny(query, page, MaxHits, BuildMetaData(), type);

    private SearchMetaData BuildMetaData() => new()
    {
        Client = QueryClient.Mcp,
        Actor = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
    };
}

public record InjuredPlayerSummary(
    int Id,
    string WebName,
    string? Status,
    string? News,
    int? ChanceOfPlayingNextRound,
    double OwnershipPercentage,
    int TeamId);

public record PriceChangesResponse(
    IEnumerable<PlayerWithPriceChange> AlreadyChanged,
    IEnumerable<PlayerLikelyPriceChange> LikelyToChange);
