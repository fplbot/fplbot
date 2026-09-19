using FplBot.WebApi.Endpoints.Api.Admin;

namespace FplBot.Tests.E2E.ApiEndpoints;

// Exercises the dependency health endpoint over HTTP against the real Redis/Elasticsearch the
// app is wired to, rather than faking either client.
[Collection("AppSearch")]
public class AdminHealthEndpointsTests(SearchAppFixture appFixture)
{
    [Fact(Skip = "Skipped: the Elasticsearch fixture is the slowest in the suite and these fail locally on leaked indices.")]
    public async Task GetDependencyHealth_BothReachable_ReturnsHealthy()
    {
        var health = await appFixture.GetJson<DependencyHealthResponse>("/api/admin/health/dependencies");

        Assert.True(health.Healthy, DescribeFailures(health));
        Assert.All(health.Dependencies, d => Assert.True(d.Healthy));
    }

    private static string DescribeFailures(DependencyHealthResponse response) =>
        string.Join(", ", response.Dependencies.Select(d => $"{d.Name}={d.Healthy}:{d.Error}"));
}
