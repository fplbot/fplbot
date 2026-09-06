using System.Net.Security;
using StackExchange.Redis;

internal static class DevSeeder
{
    private const string AllSubs =
        "All Standings Captains Transfers FixtureGoals FixtureAssists FixtureCards " +
        "FixturePenaltyMisses FixtureFullTime Taunts PriceChanges InjuryUpdates " +
        "Deadlines Lineups NewPlayers FixtureRemovedFromGameweek";

    public static Task SeedAsync(ResourceEndpointsAllocatedEvent evt, CancellationToken ct)
    {
        _ = Task.Run(() => SeedInBackgroundAsync(ct), CancellationToken.None);
        return Task.CompletedTask;
    }

    private static async Task SeedInBackgroundAsync(CancellationToken ct)
    {
        try
        {
        Console.WriteLine("[DevSeeder] Starting background seed...");

        var options = new ConfigurationOptions
        {
            EndPoints = { "localhost:6379" },
            Password = "devpassword",
            Ssl = true,
            SslClientAuthenticationOptions = _ => new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            },
            AbortOnConnectFail = false,
        };

        IConnectionMultiplexer? mux = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                mux = await ConnectionMultiplexer.ConnectAsync(options);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DevSeeder] Attempt {attempt} failed: {ex.Message}");
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
        catch (Exception ex)
        {
            Console.WriteLine($"[DevSeeder] Unhandled exception: {ex}");
        }
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
        Console.WriteLine("[DevSeeder] Inserted Slack workspace TeamId-DEV-SLACK (league 12345, channel C0DEV000001).");
    }

    private static async Task SeedDiscord(IDatabase db)
    {
        await db.HashSetAsync("Guild-111222333444555666", [
            new HashEntry("name", "Dev Discord Guild")
        ]);
        Console.WriteLine("[DevSeeder] Inserted Discord guild Guild-111222333444555666.");

        await db.HashSetAsync("GuildSubs-111222333444555666-Channel-999888777666555444", [
            new HashEntry("guildid", "111222333444555666"),
            new HashEntry("channelid", "999888777666555444"),
            new HashEntry("leagueid", "12345"),
            new HashEntry("subs", AllSubs)
        ]);
        Console.WriteLine("[DevSeeder] Inserted Discord subscription GuildSubs-111222333444555666-Channel-999888777666555444 (league 12345).");
    }
}
