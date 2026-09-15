using System.Diagnostics;
using Bogus;
using FplBot.Data.Discord;
using FplBot.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FplBot.Tests.LoadTests;

public class DiscordGuildRepositoryLoadHarness
{
    [Fact(Skip = "Manual perf investigation for the GetAllInstallations() N+1 fix — run locally, not part of CI")]
    public async Task GetAllInstallations_TimingAndRoundTrips()
    {
        const int guildCount = 2000;
        var faker = new Faker();
        var events = Enum.GetValues<FplEvent>();

        var redisContainer = new RedisBuilder("redis:latest").Build();
        await redisContainer.StartAsync(TestContext.Current.CancellationToken);
        var multiplexer = await ConnectionMultiplexer.ConnectAsync(redisContainer.GetConnectionString());
        var repo = new DiscordGuildRepository(multiplexer, NullLogger<DiscordGuildRepository>.Instance);

        for (var i = 0; i < guildCount; i++)
        {
            var installation = Installation.Install($"guild-{i}", $"Guild {i}");
            var channelCount = faker.Random.Int(0, 20);
            for (var c = 0; c < channelCount; c++)
            {
                var subscribedEvents = faker.PickRandom(events, faker.Random.Int(1, 3)).ToArray();
                installation.Subscribe($"channel-{i}-{c}", subscribedEvents);
            }
            await repo.Save(installation);
        }

        var operationsBefore = multiplexer.GetCounters().Interactive.OperationCount;
        var stopwatch = Stopwatch.StartNew();
        var installations = (await repo.GetAllInstallations()).ToList();
        stopwatch.Stop();
        var roundTrips = multiplexer.GetCounters().Interactive.OperationCount - operationsBefore;

        Assert.Equal(guildCount, installations.Count);

        const int assumedConcurrency = 64;
        Console.WriteLine($"guilds={guildCount} wallClockMs={stopwatch.ElapsedMilliseconds} roundTrips={roundTrips}");
        foreach (var assumedRttMs in new[] { 2, 5, 10 })
        {
            var sequentialMs = roundTrips * assumedRttMs;
            var concurrentMs = roundTrips / assumedConcurrency * assumedRttMs;
            Console.WriteLine($"  at {assumedRttMs}ms RTT: sequential ~{sequentialMs}ms ({sequentialMs / 1000.0:F1}s), at concurrency={assumedConcurrency} ~{concurrentMs}ms ({concurrentMs / 1000.0:F1}s)");
        }

        await redisContainer.DisposeAsync();
    }
}
