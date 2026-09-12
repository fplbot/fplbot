using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;
using FplBot.WebApi.Endpoints.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Nest;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FplBot.Tests.E2E.Health;

// Exercises AdminHealthEndpoints against real Redis/Elasticsearch — including actually
// stopping a container mid-test to simulate an outage — rather than faking either client.
[Collection("Elasticsearch")]
public class AdminHealthEndpointsTests(ElasticsearchFixture elastic)
{
    [Fact]
    public async Task GetDependencyHealth_BothReachable_ReturnsHealthy()
    {
        var result = await AdminHealthEndpoints.GetDependencyHealth(Factory.RedisConnection.Value, elastic.Client);

        var ok = Assert.IsType<Ok<DependencyHealthResponse>>(result);
        Assert.True(ok.Value!.Healthy, DescribeFailures(ok.Value));
        Assert.All(ok.Value.Dependencies, d => Assert.True(d.Healthy));
    }

    [Fact]
    public async Task GetDependencyHealth_RedisUnreachable_ReportsUnhealthy()
    {
        // A dedicated, throwaway Redis container that we deliberately stop mid-test — a real
        // outage, not a fake IConnectionMultiplexer — while leaving the shared Elasticsearch
        // fixture untouched so only the Redis dependency flips to unhealthy.
        var redisContainer = new RedisBuilder("redis:latest").Build();
        await redisContainer.StartAsync(TestContext.Current.CancellationToken);
        var redis = await ConnectionMultiplexer.ConnectAsync(redisContainer.GetConnectionString());

        var beforeStop = await AdminHealthEndpoints.GetDependencyHealth(redis, elastic.Client);
        var beforeStopOk = Assert.IsType<Ok<DependencyHealthResponse>>(beforeStop);
        Assert.True(beforeStopOk.Value!.Healthy, DescribeFailures(beforeStopOk.Value));

        await redisContainer.StopAsync(TestContext.Current.CancellationToken);

        var result = await AdminHealthEndpoints.GetDependencyHealth(redis, elastic.Client);

        var unavailable = Assert.IsType<JsonHttpResult<DependencyHealthResponse>>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        Assert.False(unavailable.Value!.Healthy);
        Assert.False(unavailable.Value.Dependencies.Single(d => d.Name == "Redis").Healthy);
        Assert.True(unavailable.Value.Dependencies.Single(d => d.Name == "Elasticsearch").Healthy);

        await redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetDependencyHealth_ElasticsearchUnreachable_ReportsUnhealthy()
    {
        // A real NEST client pointed at a port nothing listens on — genuinely exercises the
        // failure path of the real client, without the cost/flakiness of a second ES JVM
        // (already several containers-worth of memory pressure on this box) just to prove it
        // can't reach a server that isn't there.
        var settings = new ConnectionSettings(new Uri("http://127.0.0.1:1"))
            .RequestTimeout(TimeSpan.FromSeconds(2));
        var unreachableClient = new ElasticClient(settings);

        var result = await AdminHealthEndpoints.GetDependencyHealth(Factory.RedisConnection.Value, unreachableClient);

        var unavailable = Assert.IsType<JsonHttpResult<DependencyHealthResponse>>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
        Assert.False(unavailable.Value!.Healthy);
        Assert.False(unavailable.Value.Dependencies.Single(d => d.Name == "Elasticsearch").Healthy);
        Assert.True(unavailable.Value.Dependencies.Single(d => d.Name == "Redis").Healthy);
    }

    private static string DescribeFailures(DependencyHealthResponse response) =>
        string.Join(", ", response.Dependencies.Select(d => $"{d.Name}={d.Healthy}:{d.Error}"));
}
