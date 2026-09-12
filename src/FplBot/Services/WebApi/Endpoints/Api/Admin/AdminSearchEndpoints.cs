using Fpl.Search.Analytics;
using Fpl.Search.Data.Abstractions;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record ChangeBookmarkRequest(int Bookmark);

public static class AdminSearchEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/indexing/bookmarks", GetBookmarks);
        group.MapPost("/indexing/bookmarks/league", SetLeagueBookmark);
        group.MapPost("/indexing/bookmarks/entry", SetEntryBookmark);
        group.MapGet("/search/analytics", GetSearchAnalytics);
    }

    private static async Task<IResult> GetBookmarks(ILeagueIndexBookmarkProvider leagueBookmarks, IEntryIndexBookmarkProvider entryBookmarks)
    {
        return TypedResults.Ok(new
        {
            leagueIndexingBookmark = await leagueBookmarks.GetBookmark(),
            entryIndexingBookmark = await entryBookmarks.GetBookmark()
        });
    }

    private static async Task<IResult> SetLeagueBookmark(ChangeBookmarkRequest request, ILeagueIndexBookmarkProvider leagueBookmarks)
    {
        await leagueBookmarks.SetBookmark(request.Bookmark);
        return TypedResults.Ok(new { message = "League bookmark updated" });
    }

    private static async Task<IResult> SetEntryBookmark(ChangeBookmarkRequest request, IEntryIndexBookmarkProvider entryBookmarks)
    {
        await entryBookmarks.SetBookmark(request.Bookmark);
        return TypedResults.Ok(new { message = "Entry bookmark updated" });
    }

    internal static async Task<IResult> GetSearchAnalytics(int? days, int? size, ISearchAnalyticsService analyticsService)
    {
        var result = await analyticsService.GetTopSearches(days is > 0 ? days.Value : 7, size is > 0 ? size.Value : 20);
        return TypedResults.Ok(result);
    }
}
