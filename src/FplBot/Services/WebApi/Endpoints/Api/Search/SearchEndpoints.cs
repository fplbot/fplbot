using Fpl.Search.Models;
using Fpl.Search.Searching;

namespace FplBot.WebApi.Endpoints.Api.Search;

public static class SearchEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/entries/{id:int}", GetEntry);
        group.MapGet("/entries", GetEntries);
        group.MapGet("/leagues", GetLeagues);
        group.MapGet("/any", GetAny);
    }

    private static async Task<IResult> GetEntry(int id, ISearchService searchService)
    {
        var entry = await searchService.GetEntry(id);
        return entry == null ? TypedResults.NotFound() : TypedResults.Ok(entry);
    }

    private static async Task<IResult> GetEntries(string query, int page, HttpContext httpContext, ISearchService searchService)
    {
        var metaData = new SearchMetaData
        {
            Client = QueryClient.Web, Actor = httpContext.Connection.RemoteIpAddress?.ToString()
        };

        var searchResult = await searchService.SearchForEntry(query, page, 10, metaData);

        if (searchResult.TotalPages < page && !searchResult.Any())
        {
            return TypedResults.BadRequest(new { errors = new { page = new[] { $"{nameof(page)} exceeds the total page count" } } });
        }

        return TypedResults.Ok(new { Hits = searchResult });
    }

    private static async Task<IResult> GetLeagues(string query, int page, string countryToBoost, HttpContext httpContext, ISearchService searchService)
    {
        var metaData = new SearchMetaData
        {
            Client = QueryClient.Web, Actor = httpContext.Connection.RemoteIpAddress?.ToString()
        };

        var searchResult = await searchService.SearchForLeague(query, page, 10, metaData, countryToBoost);

        if (searchResult.TotalPages < page && !searchResult.Any())
        {
            return TypedResults.BadRequest(new { errors = new { page = new[] { $"{nameof(page)} exceeds the total page count" } } });
        }

        return TypedResults.Ok(new { Hits = searchResult });
    }

    private static async Task<IResult> GetAny(string query, int page, HttpContext httpContext, ISearchService searchService, SearchType type = SearchType.All)
    {
        var metaData = new SearchMetaData
        {
            Client = QueryClient.Web, Actor = httpContext.Connection.RemoteIpAddress?.ToString()
        };

        var searchResult = await searchService.SearchAny(query, page, 10, metaData, type);

        if (searchResult.TotalPages < page && !searchResult.Any())
        {
            return TypedResults.BadRequest(new { errors = new { page = new[] { $"{nameof(page)} exceeds the total page count" } } });
        }

        return TypedResults.Ok(new { Hits = searchResult });
    }
}
