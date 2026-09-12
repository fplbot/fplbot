using StackExchange.Redis;
using Testcontainers.Redis;

namespace FplBot.Tests.Data;

// One real Redis container, shared across every test in RedisIntegrationTests (mirrors
// EventHandlerFixture's and ElasticsearchFixture's shared-container pattern). Each test
// flushes the database before it runs so tests never see leftover state from another test.
public class RedisIntegrationFixture : IAsyncLifetime
{
    // Local-only: set REUSE_TEST_CONTAINERS=true to keep this container warm across
    // `dotnet test` runs instead of tearing it down each time. Never set in CI.
    private static readonly bool ReuseContainers =
        Environment.GetEnvironmentVariable("REUSE_TEST_CONTAINERS") == "true";

    private readonly RedisContainer _container = new RedisBuilder("redis:latest")
        .WithReuse(ReuseContainers)
        .WithLabel("reuse-id", "redis-integration-fixture")
        .Build();

    public IConnectionMultiplexer Multiplexer { get; private set; } = null!;
    public IServer Server { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        Multiplexer = await ConnectionMultiplexer.ConnectAsync(ConnectionString + ",allowAdmin=true");
        Server = Multiplexer.GetServer(ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        if (!ReuseContainers) await _container.DisposeAsync();
    }
}
