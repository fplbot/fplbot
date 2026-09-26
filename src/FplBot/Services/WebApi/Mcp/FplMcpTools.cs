using System.ComponentModel;
using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Models.Mappers;
using Fpl.Search.Models;
using Fpl.Search.Searching;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.WebApi.Endpoints.Api.Fpl;
using ModelContextProtocol;
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
    ICaptainsByGameWeek captainsByGameWeek,
    ITransfersByGameWeek transfersByGameWeek,
    IFixtureClient fixtureClient,
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

    [McpServerTool(Name = "get_captains", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Captain and vice-captain picks for every entry in a classic league for a given gameweek (defaults to the current gameweek).")]
    public async Task<IEnumerable<EntryCaptainPick>> GetCaptains(
        [Description("The classic league's FPL id")] int leagueId,
        [Description("Gameweek number; defaults to the current gameweek if omitted")] int? gameweek = null)
    {
        try
        {
            return await captainsByGameWeek.GetEntryCaptainPicks(await ResolveGameweek(gameweek), leagueId);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            throw new McpException($"No league found with id {leagueId}.");
        }
    }

    [McpServerTool(Name = "get_transfers", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Transfers made by every entry in a classic league for a given gameweek (defaults to the current gameweek).")]
    public async Task<IEnumerable<TransfersByGameWeek.Transfer>> GetTransfers(
        [Description("The classic league's FPL id")] int leagueId,
        [Description("Gameweek number; defaults to the current gameweek if omitted")] int? gameweek = null) =>
        await transfersByGameWeek.GetTransfersByGameweek(await ResolveGameweek(gameweek), leagueId);

    [McpServerTool(Name = "get_gameweek", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Gameweek info (id, name, deadline UTC, time remaining until deadline as untilDeadline, fixtures) for the previous/current/next gameweek, or a specific gameweek by id. untilDeadline is omitted once the deadline has passed. Fixtures include short team codes (e.g. WHU-CHE) - use those, not full names, to match fplbot's Slack/Discord bot conventions.")]
    public async Task<GameweekResponse> GetGameweek(
        [Description("Specific gameweek id to look up; if omitted, returns previous/current/next instead")] int? gameweekId = null)
    {
        var settings = await globalSettingsClient.GetGlobalSettings();
        var gameweeks = settings?.Gameweeks ?? [];
        var teamsById = (settings?.Teams ?? []).ToDictionary(t => t.Id);

        if (gameweekId is { } requestedId)
        {
            var requested = gameweeks.SingleOrDefault(g => g.Id == requestedId)
                ?? throw new McpException($"No gameweek found with id {requestedId}.");
            return new GameweekResponse(null, null, null, await ToGameweekWithFixtures(requested, teamsById));
        }

        var previousTask = ToGameweekWithFixtures(gameweeks.GetPreviousGameweek(), teamsById);
        var currentTask = ToGameweekWithFixtures(gameweeks.GetCurrentGameweek(), teamsById);
        var nextTask = ToGameweekWithFixtures(gameweeks.GetNextGameweek(), teamsById);
        await Task.WhenAll(previousTask, currentTask, nextTask);

        return new GameweekResponse(await previousTask, await currentTask, await nextTask, null);
    }

    private async Task<GameweekWithFixtures?> ToGameweekWithFixtures(Gameweek? gameweek, IReadOnlyDictionary<int, Team> teamsById)
    {
        if (gameweek == null) return null;

        var fixtures = await fixtureClient.GetFixturesByGameweek(gameweek.Id) ?? [];
        var untilDeadline = gameweek.Deadline - DateTime.UtcNow;
        return new GameweekWithFixtures(
            gameweek.Id,
            gameweek.Name,
            gameweek.Deadline,
            untilDeadline > TimeSpan.Zero ? untilDeadline : null,
            gameweek.IsFinished,
            fixtures.Select(f => new FixtureSummary(
                f.Id,
                f.HomeTeamId,
                f.AwayTeamId,
                teamsById.GetValueOrDefault(f.HomeTeamId)?.ShortName ?? "",
                teamsById.GetValueOrDefault(f.AwayTeamId)?.ShortName ?? "",
                f.KickOffTime,
                f.Finished,
                f.HomeTeamDifficulty,
                f.AwayTeamDifficulty)));
    }

    [McpServerTool(Name = "get_fixture_difficulty", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("A team's fixtures and difficulty ratings for the next N gameweeks (default 5), identified by team id, team name, or a player on it. An empty fixtures list for a gameweek means a blank gameweek for this team; two or more means a double gameweek.")]
    public async Task<TeamFixtureDifficulty> GetFixtureDifficulty(
        [Description("The team's FPL id")] int? teamId = null,
        [Description("The team's name or short name, e.g. \"Brighton\" or \"BHA\"")] string? teamName = null,
        [Description("A player's name - resolves to their team")] string? playerName = null,
        [Description("How many gameweeks ahead to include, starting from the current gameweek")] int gameweeksAhead = 5)
    {
        var identifierCount = new[] { teamId.HasValue, !string.IsNullOrWhiteSpace(teamName), !string.IsNullOrWhiteSpace(playerName) }.Count(x => x);
        if (identifierCount != 1)
        {
            throw new McpException("Provide exactly one of teamId, teamName, or playerName.");
        }

        var settings = await globalSettingsClient.GetGlobalSettings();
        var teams = settings?.Teams ?? [];
        Player? matchedPlayer = null;
        Team team;

        if (teamId.HasValue)
        {
            team = teams.SingleOrDefault(t => t.Id == teamId.Value)
                ?? throw new McpException($"No team found with id {teamId.Value}.");
        }
        else if (!string.IsNullOrWhiteSpace(teamName))
        {
            team = teams.SingleOrDefault(t =>
                    string.Equals(t.Name, teamName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.ShortName, teamName, StringComparison.OrdinalIgnoreCase))
                ?? throw new McpException($"No team found matching \"{teamName}\".");
        }
        else
        {
            matchedPlayer = playerSearch.FindMostPopularMatchingPlayer(settings?.Players ?? [], playerName!)
                ?? throw new McpException($"No player found matching \"{playerName}\".");
            team = teams.SingleOrDefault(t => t.Code == matchedPlayer.TeamCode)
                ?? throw new McpException($"Matched player {matchedPlayer.WebName}, but could not resolve their team.");
        }

        var (startGameweekId, gameweekCount, fixtures) = await GetUpcomingFixtures(gameweeksAhead);

        var gameweeks = Enumerable.Range(startGameweekId, gameweekCount).Select(gwId =>
        {
            var teamFixtures = fixtures.Where(f => f.Event == gwId && (f.HomeTeamId == team.Id || f.AwayTeamId == team.Id));
            var difficulties = teamFixtures.Select(f =>
            {
                var isHome = f.HomeTeamId == team.Id;
                var opponentId = isHome ? f.AwayTeamId : f.HomeTeamId;
                var opponent = teams.SingleOrDefault(t => t.Id == opponentId);
                return new FixtureDifficulty(
                    opponentId,
                    opponent?.ShortName ?? "",
                    isHome,
                    isHome ? f.HomeTeamDifficulty : f.AwayTeamDifficulty);
            });
            return new GameweekFixtures(gwId, difficulties);
        });

        return new TeamFixtureDifficulty(team.Id, team.Name ?? "", matchedPlayer, gameweeks);
    }

    [McpServerTool(Name = "get_double_and_blank_gameweeks", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Which teams have a blank (no fixture) or double (2+ fixtures) gameweek in the next N gameweeks (default 5).")]
    public async Task<DoubleAndBlankGameweeksResponse> GetDoubleAndBlankGameweeks(
        [Description("How many gameweeks ahead to include, starting from the current gameweek")] int gameweeksAhead = 5)
    {
        var settings = await globalSettingsClient.GetGlobalSettings();
        var teams = settings?.Teams ?? [];

        var (startGameweekId, gameweekCount, fixtures) = await GetUpcomingFixtures(gameweeksAhead);

        var gameweeks = Enumerable.Range(startGameweekId, gameweekCount).Select(gwId =>
        {
            var fixturesThisGameweek = fixtures.Where(f => f.Event == gwId).ToArray();
            var fixtureCountByTeam = teams.ToDictionary(
                t => t.Id,
                t => fixturesThisGameweek.Count(f => f.HomeTeamId == t.Id || f.AwayTeamId == t.Id));

            var blankTeams = teams.Where(t => fixtureCountByTeam[t.Id] == 0).Select(t => new TeamRef(t.Id, t.ShortName ?? ""));
            var doubleTeams = teams.Where(t => fixtureCountByTeam[t.Id] >= 2).Select(t => new TeamRef(t.Id, t.ShortName ?? ""));

            return new GameweekTeamCounts(gwId, blankTeams, doubleTeams);
        });

        return new DoubleAndBlankGameweeksResponse(gameweeks);
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

    private async Task<int> ResolveGameweek(int? gameweek)
    {
        if (gameweek is { } explicitGameweek) return explicitGameweek;

        var settings = await globalSettingsClient.GetGlobalSettings();
        var current = settings?.Gameweeks.GetCurrentGameweek();
        return current?.Id ?? throw new McpException("No current gameweek — the season may be between gameweeks.");
    }

    private async Task<(int StartGameweekId, int GameweekCount, ICollection<Fixture> Fixtures)> GetUpcomingFixtures(int gameweeksAhead)
    {
        if (gameweeksAhead < 1)
        {
            throw new McpException("gameweeksAhead must be at least 1.");
        }

        var settings = await globalSettingsClient.GetGlobalSettings();
        var startGameweek = settings?.Gameweeks.GetCurrentGameweek() ?? settings?.Gameweeks.GetNextGameweek()
            ?? throw new McpException("No current or next gameweek — the season may be over.");

        var lastGameweekId = settings!.Gameweeks.Max(g => g.Id);
        var endGameweekId = Math.Min(startGameweek.Id + gameweeksAhead - 1, lastGameweekId);
        var gameweekCount = endGameweekId - startGameweek.Id + 1;

        var allFixtures = await fixtureClient.GetFixtures() ?? [];
        var upcoming = allFixtures.Where(f => f.Event is { } gw && gw >= startGameweek.Id && gw <= endGameweekId).ToArray();

        return (startGameweek.Id, gameweekCount, upcoming);
    }

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

public record GameweekResponse(
    GameweekWithFixtures? Previous,
    GameweekWithFixtures? Current,
    GameweekWithFixtures? Next,
    GameweekWithFixtures? Requested);

public record GameweekWithFixtures(
    int Id,
    string? Name,
    DateTime Deadline,
    TimeSpan? UntilDeadline,
    bool IsFinished,
    IEnumerable<FixtureSummary> Fixtures);

public record FixtureSummary(
    int Id,
    int HomeTeamId,
    int AwayTeamId,
    string HomeTeamShortName,
    string AwayTeamShortName,
    DateTime? KickOffTime,
    bool Finished,
    int HomeTeamDifficulty,
    int AwayTeamDifficulty);

public record TeamFixtureDifficulty(int TeamId, string TeamName, Player? MatchedPlayer, IEnumerable<GameweekFixtures> Gameweeks);

public record GameweekFixtures(int GameweekId, IEnumerable<FixtureDifficulty> Fixtures);

public record FixtureDifficulty(int OpponentTeamId, string OpponentShortName, bool IsHome, int Difficulty);

public record DoubleAndBlankGameweeksResponse(IEnumerable<GameweekTeamCounts> Gameweeks);

public record GameweekTeamCounts(int GameweekId, IEnumerable<TeamRef> BlankTeams, IEnumerable<TeamRef> DoubleTeams);

public record TeamRef(int TeamId, string ShortName);
