using System.Net.Http.Headers;
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

    private static readonly string[] ConcreteEvents =
    [
        "Standings", "Captains", "Transfers", "FixtureGoals", "FixtureAssists", "FixtureCards",
        "FixturePenaltyMisses", "FixtureFullTime", "Taunts", "PriceChanges", "InjuryUpdates",
        "Deadlines", "Lineups", "NewPlayers", "FixtureRemovedFromGameweek"
    ];

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
        await SeedSlackWorkspace(db, "DEV-SLACK", "Dev Slack Workspace", "xoxb-dev-fake-token", "C0DEV000001", 12345, AllSubs);
        await SeedSlackWorkspace(db, "DEV-SLACK-2", "Dev Slack Workspace 2", "xoxb-dev-fake-token-2", "C0DEV000002", 23456, "Standings Captains Transfers");
        await SeedSlackWorkspace(db, "DEV-SLACK-3", "Dev Slack Workspace 3", "xoxb-dev-fake-token-3", "C0DEV000003", 34567, "PriceChanges InjuryUpdates Deadlines");
        await SeedSlackWorkspace(db, "T0C2TLMHKDK", "fplbotdev-throwaway-slack", "xoxb-dev-fake-token-throwaway", "C0C2YFF57HQ", 12345, AllSubs);

        // No channel subscriptions at all — a bare install to exercise the "no channels" path in the admin UI.
        await db.HashSetAsync("TeamId-DEV-SLACK-BARE", [
            new HashEntry("accessToken", "xoxb-dev-fake-token-bare"),
            new HashEntry("teamName", "Dev Slack Workspace (bare install)")
        ]);
        Console.WriteLine("[DevSeeder] Inserted Slack workspace TeamId-DEV-SLACK-BARE (no channel subscriptions).");
    }

    private static async Task SeedSlackWorkspace(IDatabase db, string teamId, string teamName, string accessToken, string channelId, int leagueId, string subscriptions)
    {
        var teamKey = $"TeamId-{teamId}";
        await db.HashSetAsync(teamKey, [
            new HashEntry("accessToken", accessToken),
            new HashEntry("teamName", teamName)
        ]);

        var channelKey = $"SlackChannelSub-{teamId}-{channelId}";
        await db.HashSetAsync(channelKey, [
            new HashEntry("teamId", teamId),
            new HashEntry("channelId", channelId),
            new HashEntry("leagueId", leagueId.ToString()),
            new HashEntry("subscriptions", subscriptions)
        ]);
        await db.SetAddAsync($"SlackChannelSubIndex-{teamId}", channelId);
        await db.SetAddAsync("TeamIndex", teamId);
        await SeedEventIndex(db, "SlackEventIndex", teamId, channelId, subscriptions);

        Console.WriteLine($"[DevSeeder] Inserted Slack workspace {teamKey} (league {leagueId}, channel {channelId}).");
    }

    private static async Task SeedDiscord(IDatabase db)
    {
        await SeedDiscordGuild(db, "111222333444555666", "Dev Discord Guild", "999888777666555444", 12345, AllSubs);
        await SeedDiscordGuild(db, "1546966580007542937", "fplbotdev-throwaway-discord", "1546966580976549940", 12345, AllSubs);
    }

    private static async Task SeedDiscordGuild(IDatabase db, string guildId, string name, string channelId, int leagueId, string subscriptions)
    {
        await db.HashSetAsync($"Guild-{guildId}", [
            new HashEntry("name", name)
        ]);
        await db.SetAddAsync("GuildIndex", guildId);

        await db.HashSetAsync($"GuildSubs-{guildId}-Channel-{channelId}", [
            new HashEntry("guildid", guildId),
            new HashEntry("channelid", channelId),
            new HashEntry("leagueid", leagueId.ToString()),
            new HashEntry("subs", subscriptions)
        ]);
        await db.SetAddAsync($"GuildChannelSubIndex-{guildId}", channelId);
        await SeedEventIndex(db, "GuildEventIndex", guildId, channelId, subscriptions);

        Console.WriteLine($"[DevSeeder] Inserted Discord guild Guild-{guildId} (league {leagueId}, channel {channelId}).");
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
