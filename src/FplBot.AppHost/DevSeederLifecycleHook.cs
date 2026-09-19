using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;

internal static class DevSeeder
{
    private static readonly string[] ConcreteEvents =
    [
        "Standings", "Captains", "Transfers", "FixtureGoals", "FixtureAssists", "FixtureCards",
        "FixturePenaltyMisses", "FixtureFullTime", "Taunts", "PriceChanges", "InjuryUpdates",
        "Deadlines", "Lineups", "NewPlayers", "FixtureRemovedFromGameweek"
    ];

    public static string SlackToken { get; set; } = "xoxb-dev-fake-token-throwaway";

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
        Console.WriteLine($"[DevSeeder] Slack token for T0C2TLMHKDK: {SlackToken[..Math.Min(12, SlackToken.Length)]}... (set DEV_SEED_SLACK_TOKEN in FplBot.AppHost user secrets for a real one)");

        await SeedSlackWorkspace(db, "DEV-SLACK", "Dev Slack Workspace", "xoxb-dev-fake-token", "C0DEV000001", 12345, AllSubs,
            "5eed0000000000000000000000000001", "5eed0000000000000000000000001001");
        await SeedSlackWorkspace(db, "DEV-SLACK-2", "Dev Slack Workspace 2", "xoxb-dev-fake-token-2", "C0DEV000002", 23456, "Standings Captains Transfers",
            "5eed0000000000000000000000000002", "5eed0000000000000000000000001002");
        await SeedSlackWorkspace(db, "DEV-SLACK-3", "Dev Slack Workspace 3", "xoxb-dev-fake-token-3", "C0DEV000003", 34567, "PriceChanges InjuryUpdates Deadlines",
            "5eed0000000000000000000000000003", "5eed0000000000000000000000001003");
        await SeedSlackWorkspace(db, "T0C2TLMHKDK", "fplbotdev-throwaway-slack", SlackToken, "C0C2YFF57HQ", 555, AllSubs,
            "5eed0000000000000000000000000004", "5eed0000000000000000000000001004");

        // No channel subscriptions at all — a bare install to exercise the "no channels" path in the admin UI.
        await db.HashSetAsync("TeamId-DEV-SLACK-BARE", [
            new HashEntry("accessToken", "xoxb-dev-fake-token-bare"),
            new HashEntry("teamName", "Dev Slack Workspace (bare install)")
        ]);
        await SeedInstallationId(db, "slack", "TeamId-DEV-SLACK-BARE", "DEV-SLACK-BARE", "5eed0000000000000000000000000005");
        Console.WriteLine("[DevSeeder] Inserted Slack workspace TeamId-DEV-SLACK-BARE (no channel subscriptions).");
    }

    private static async Task SeedSlackWorkspace(IDatabase db, string teamId, string teamName, string accessToken, string channelId, int leagueId,
        string subscriptions, string installationId, string subscriptionId)
    {
        var teamKey = $"TeamId-{teamId}";
        await db.HashSetAsync(teamKey, [
            new HashEntry("accessToken", accessToken),
            new HashEntry("teamName", teamName)
        ]);
        await SeedInstallationId(db, "slack", teamKey, teamId, installationId);

        var channelKey = $"SlackChannelSub-{teamId}-{channelId}";
        await db.HashSetAsync(channelKey, [
            new HashEntry("teamId", teamId),
            new HashEntry("channelId", channelId),
            new HashEntry("leagueId", leagueId.ToString()),
            new HashEntry("subscriptions", subscriptions)
        ]);
        await SeedSubscriptionId(db, "slack", channelKey, teamId, channelId, subscriptionId);
        await db.SetAddAsync($"SlackChannelSubIndex-{teamId}", channelId);
        await db.SetAddAsync("TeamIndex", teamId);
        await SeedEventIndex(db, "SlackEventIndex", teamId, channelId, subscriptions);

        Console.WriteLine($"[DevSeeder] Inserted Slack workspace {teamKey} (league {leagueId}, channel {channelId}).");
    }

    private static async Task SeedDiscord(IDatabase db)
    {
        await db.HashSetAsync("Guild-111222333444555666", [
            new HashEntry("name", "Dev Discord Guild")
        ]);
        await db.SetAddAsync("GuildIndex", "111222333444555666");
        await SeedInstallationId(db, "discord", "Guild-111222333444555666", "111222333444555666", "5eed0000000000000000000000000006");
        Console.WriteLine("[DevSeeder] Inserted Discord guild Guild-111222333444555666.");

        await db.HashSetAsync("GuildSubs-111222333444555666-Channel-999888777666555444", [
            new HashEntry("guildid", "111222333444555666"),
            new HashEntry("channelid", "999888777666555444"),
            new HashEntry("leagueid", "12345"),
            new HashEntry("subs", AllSubs)
        ]);
        await db.SetAddAsync("GuildChannelSubIndex-111222333444555666", "999888777666555444");
        await SeedSubscriptionId(db, "discord", "GuildSubs-111222333444555666-Channel-999888777666555444", "111222333444555666", "999888777666555444", "5eed0000000000000000000000001005");
        await SeedEventIndex(db, "GuildEventIndex", "111222333444555666", "999888777666555444", AllSubs);
        Console.WriteLine("[DevSeeder] Inserted Discord subscription GuildSubs-111222333444555666-Channel-999888777666555444 (league 12345).");

        await db.HashSetAsync("Guild-1546966580007542937", [
            new HashEntry("name", "fplbotdev-throwaway-discord")
        ]);
        await db.SetAddAsync("GuildIndex", "1546966580007542937");
        await SeedInstallationId(db, "discord", "Guild-1546966580007542937", "1546966580007542937", "5eed0000000000000000000000000007");
        Console.WriteLine("[DevSeeder] Inserted Discord guild Guild-1546966580007542937.");

        await db.HashSetAsync("GuildSubs-1546966580007542937-Channel-1546966580976549940", [
            new HashEntry("guildid", "1546966580007542937"),
            new HashEntry("channelid", "1546966580976549940"),
            new HashEntry("leagueid", "12345"),
            new HashEntry("subs", AllSubs)
        ]);
        await db.SetAddAsync("GuildChannelSubIndex-1546966580007542937", "1546966580976549940");
        await SeedSubscriptionId(db, "discord", "GuildSubs-1546966580007542937-Channel-1546966580976549940", "1546966580007542937", "1546966580976549940", "5eed0000000000000000000000001006");
        await SeedEventIndex(db, "GuildEventIndex", "1546966580007542937", "1546966580976549940", AllSubs);
        Console.WriteLine("[DevSeeder] Inserted Discord subscription GuildSubs-1546966580007542937-Channel-1546966580976549940 (league 12345).");
    }

    private static async Task SeedInstallationId(IDatabase db, string platform, string installationKey, string externalId, string id)
    {
        await db.HashSetAsync(installationKey, "id", id);
        await db.StringSetAsync($"InstallationId-{id}", $"{platform}:{externalId}");
    }

    private static async Task SeedSubscriptionId(IDatabase db, string platform, string channelKey, string externalId, string channelId, string id)
    {
        await db.HashSetAsync(channelKey, "id", id);
        await db.StringSetAsync($"SubId-{id}", $"{platform}:{externalId}:{channelId}");
    }

    private static async Task SeedEventIndex(IDatabase db, string indexPrefix, string installationId, string channelId, string subscriptions)
    {
        var subscribed = subscriptions.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var events = subscribed.Contains("All") ? ConcreteEvents : subscribed;
        foreach (var fplEvent in events)
        {
            await db.SetAddAsync($"{indexPrefix}-{fplEvent}", $"{installationId}:{channelId}");
        }
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
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
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
