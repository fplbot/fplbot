using System.Net.Security;
using System.Text;
using System.Text.Json;
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

    public static Task SeedElasticsearchAsync(ResourceEndpointsAllocatedEvent evt, CancellationToken ct)
    {
        _ = Task.Run(() => SeedElasticsearchInBackgroundAsync(ct), CancellationToken.None);
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

    // Must match search.EntriesIndex / search.LeaguesIndex in appsettings.json.
    private const string EntriesIndex = "entries-index";
    private const string LeaguesIndex = "leagues-index";

    private static async Task SeedElasticsearchInBackgroundAsync(CancellationToken ct)
    {
        try
        {
            Console.WriteLine("[DevSeeder] Starting Elasticsearch seed...");
            using var http = new HttpClient { BaseAddress = new Uri("http://localhost:9200") };
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("elastic:dev")));

            var healthy = false;
            for (var attempt = 0; attempt < 20 && !healthy; attempt++)
            {
                try
                {
                    var health = await http.GetAsync("/_cluster/health", ct);
                    healthy = health.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DevSeeder] Elasticsearch attempt {attempt} failed: {ex.Message}");
                }

                if (!healthy)
                    await Task.Delay(1000, ct);
            }

            if (!healthy)
            {
                Console.WriteLine("[DevSeeder] Could not reach Elasticsearch, skipping seed.");
                return;
            }

            await IndexDoc(http, EntriesIndex, "1", new
            {
                id = 1,
                realName = "Magnus Carlsen",
                teamName = "Carlsen's Aces",
                alias = "MC",
                description = "Dev seed entry",
                country = "NO",
                numberOfPastSeasons = 3,
                thumbprint = "dev-seed",
            }, ct);

            await IndexDoc(http, EntriesIndex, "2", new
            {
                id = 2,
                realName = "Mohamed Salah Fan",
                teamName = "Salah Boys",
                alias = "MoSalah",
                description = "Dev seed entry",
                country = "EG",
                numberOfPastSeasons = 1,
                thumbprint = "dev-seed",
            }, ct);

            await IndexDoc(http, LeaguesIndex, "1001", new
            {
                id = 1001,
                name = "Arsenal Dev League",
                adminEntry = 1,
                adminName = "Magnus Carlsen",
                adminTeamName = "Carlsen's Aces",
                adminCountry = "NO",
            }, ct);

            await http.PostAsync($"/{EntriesIndex},{LeaguesIndex}/_refresh", null, ct);

            Console.WriteLine($"[DevSeeder] Seeded 2 entries into '{EntriesIndex}' + 1 league into '{LeaguesIndex}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DevSeeder] Elasticsearch seed unhandled exception: {ex}");
        }
    }

    private static async Task IndexDoc(HttpClient http, string index, string id, object doc, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(doc);
        var response = await http.PutAsync($"/{index}/_doc/{id}",
            new StringContent(json, Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();
    }
}
