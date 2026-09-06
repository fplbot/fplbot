using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

internal static class DevSeeder
{
    private const string AllSubs =
        "All Standings Captains Transfers FixtureGoals FixtureAssists FixtureCards " +
        "FixturePenaltyMisses FixtureFullTime Taunts PriceChanges InjuryUpdates " +
        "Deadlines Lineups NewPlayers FixtureRemovedFromGameweek";

    public static Task SeedAsync(ResourceEndpointsAllocatedEvent evt, CancellationToken ct)
    {
        // Run seeding in the background so we don't block Aspire's startup event chain
        _ = Task.Run(() => SeedInBackgroundAsync(evt, ct), CancellationToken.None);
        return Task.CompletedTask;
    }

    private static async Task SeedInBackgroundAsync(ResourceEndpointsAllocatedEvent evt, CancellationToken ct)
    {
        var notifications = evt.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceAsync("redis", KnownResourceStates.Running, ct);

        var redis = evt.Resource as RedisResource;
        if (redis == null) return;

        var endpoint = redis.GetEndpoint("tcp");
        var connectionString = $"{endpoint.Host}:{endpoint.Port},password=devpassword";

        IConnectionMultiplexer? mux = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                mux = await ConnectionMultiplexer.ConnectAsync(connectionString);
                break;
            }
            catch
            {
                await Task.Delay(500, ct);
            }
        }

        if (mux == null)
        {
            Console.WriteLine("[DevSeeder] Could not connect to Redis after 10 attempts, skipping seed.");
            return;
        }

        var db = mux.GetDatabase();
        await SeedSlack(db);
        await SeedDiscord(db);

        Console.WriteLine("[DevSeeder] Seeded fake Slack workspace and Discord guild into Redis.");
    }

    private static async Task SeedSlack(IDatabase db)
    {
        await db.HashSetAsync("TeamId-DEV-SLACK", [
            new HashEntry("accessToken", "xoxb-dev-fake-token"),
            new HashEntry("fplchannel", "C0DEV000001"),
            new HashEntry("fplleagueId", "12345"),
            new HashEntry("teamName", "Dev Slack Workspace"),
            new HashEntry("subscriptions", AllSubs)
        ]);
    }

    private static async Task SeedDiscord(IDatabase db)
    {
        await db.HashSetAsync("Guild-111222333444555666", [
            new HashEntry("name", "Dev Discord Guild")
        ]);

        await db.HashSetAsync("GuildSubs-111222333444555666-Channel-999888777666555444", [
            new HashEntry("guildid", "111222333444555666"),
            new HashEntry("channelid", "999888777666555444"),
            new HashEntry("leagueid", "12345"),
            new HashEntry("subs", AllSubs)
        ]);
    }
}
