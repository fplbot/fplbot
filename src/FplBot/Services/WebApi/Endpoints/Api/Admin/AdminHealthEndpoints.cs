using Nest;
using StackExchange.Redis;

namespace FplBot.WebApi.Endpoints.Api.Admin;

public record DependencyHealth(string Name, bool Healthy, string? Error);

public record DependencyHealthResponse(bool Healthy, IReadOnlyList<DependencyHealth> Dependencies);

public static class AdminHealthEndpoints
{
    public static void Map(RouteGroupBuilder group, IWebHostEnvironment env)
    {
        var route = group.MapGet("/health/dependencies", GetDependencyHealth);

        // /debug only reports build/version info and can't see a broken Redis/Elasticsearch
        // connection — this is the endpoint that catches that, so it needs to be reachable
        // without an admin cookie while developing locally.
        if (env.IsDevelopment())
        {
            route.AllowAnonymous();
        }
    }

    internal static async Task<IResult> GetDependencyHealth(IConnectionMultiplexer redis, IElasticClient elasticClient)
    {
        var dependencies = new[]
        {
            await CheckRedis(redis),
            await CheckElasticsearch(elasticClient)
        };

        var response = new DependencyHealthResponse(dependencies.All(d => d.Healthy), dependencies);
        if (response.Healthy)
        {
            return TypedResults.Ok(response);
        }

        return TypedResults.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<DependencyHealth> CheckRedis(IConnectionMultiplexer redis)
    {
        try
        {
            await redis.GetDatabase().PingAsync();
            return new DependencyHealth("Redis", true, null);
        }
        catch (Exception e)
        {
            return new DependencyHealth("Redis", false, e.Message);
        }
    }

    private static async Task<DependencyHealth> CheckElasticsearch(IElasticClient elasticClient)
    {
        try
        {
            var response = await elasticClient.PingAsync();
            return new DependencyHealth("Elasticsearch", response.IsValid, response.IsValid ? null : response.DebugInformation);
        }
        catch (Exception e)
        {
            return new DependencyHealth("Elasticsearch", false, e.Message);
        }
    }
}
