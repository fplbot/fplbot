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
            await SeedWebPushSubscribers(db);

            Console.WriteLine("[DevSeeder] Seeded fake Slack workspaces, Discord guilds, and Web Push subscribers into Redis.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DevSeeder] Unhandled exception: {ex}");
        }
    }

    private static async Task SeedSlack(IDatabase db)
    {
        Console.WriteLine($"[DevSeeder] Slack token for T0C2TLMHKDK: {SlackToken[..Math.Min(12, SlackToken.Length)]}... (set DEV_SEED_SLACK_TOKEN in FplBot.AppHost user secrets for a real one)");

        // Spread of channel sizes so the Slack reach stat/sort/filter on the admin dashboard have
        // something to chew on: tiny, medium, large, and one channel never swept (contributes 0).
        await SeedSlackWorkspace(db, "DEV-SLACK", "DevSeededWorkspace", "xoxb-dev-fake-token", "C0DEV000001", 12345, AllSubs,
            "5eedcafe00000000000000000000deac", "5eedbeef000000000000000001234501", memberCount: 842);
        await SeedSlackWorkspace(db, "DEV-SLACK-2", "DevSeededWorkspace 2", "xoxb-dev-fake-token-2", "C0DEV000002", 23456, "Standings Captains Transfers",
            "5eedcafe0000000000000000000deac2", "5eedbeef000000000000000002345601", memberCount: 37);
        await SeedSlackWorkspace(db, "DEV-SLACK-3", "DevSeededWorkspace 3", "xoxb-dev-fake-token-3", "C0DEV000003", 34567, "PriceChanges InjuryUpdates Deadlines",
            "5eedcafe0000000000000000000deac3", "5eedbeef000000000000000003456701", memberCount: 6210);
        await SeedSlackWorkspace(db, "T0C2TLMHKDK", "fplbotdev-throwaway-slack", SlackToken, "C0C2YFF57HQ", 555, AllSubs,
            "5eedcafe000000000000000000000c2d", "5eedbeef000000000000000000055501", memberCount: null);

        // A second channel on the same workspace as DEV-SLACK, to exercise "a team's size sums
        // across its channels" (and double-counts anyone in both).
        await SeedSlackChannel(db, "DEV-SLACK", "C0DEV000004", null, "Standings", "5eedbeef000000000000000001234504", memberCount: 118);

        // No channel subscriptions at all — a bare install to exercise the "no channels" path in the admin UI.
        await db.HashSetAsync("TeamId-DEV-SLACK-BARE", [
            new HashEntry("accessToken", "xoxb-dev-fake-token-bare"),
            new HashEntry("teamName", "DevSeededWorkspace (bare install)")
        ]);
        await SeedInstallationId(db, "slack", "TeamId-DEV-SLACK-BARE", "DEV-SLACK-BARE", "5eedcafe00000000000000000deacbae");
        Console.WriteLine("[DevSeeder] Inserted Slack workspace TeamId-DEV-SLACK-BARE (no channel subscriptions).");
    }

    private static async Task SeedSlackWorkspace(IDatabase db, string teamId, string teamName, string accessToken, string channelId, int leagueId,
        string subscriptions, string installationId, string subscriptionId, int? memberCount)
    {
        var teamKey = $"TeamId-{teamId}";
        await db.HashSetAsync(teamKey, [
            new HashEntry("accessToken", accessToken),
            new HashEntry("teamName", teamName)
        ]);
        await SeedInstallationId(db, "slack", teamKey, teamId, installationId);
        await db.SetAddAsync("TeamIndex", teamId);

        await SeedSlackChannel(db, teamId, channelId, leagueId, subscriptions, subscriptionId, memberCount);

        Console.WriteLine($"[DevSeeder] Inserted Slack workspace {teamKey} (league {leagueId}, channel {channelId}).");
    }

    // Mirrors ChannelMemberCountRepository's key format (Data/Slack/ChannelMemberCountRepository.cs):
    // count/updatedAt as separate string keys plus a SlackChannelMemberCountIndex set, so the
    // Slack reach admin endpoints read this back exactly as they would a real nightly sweep.
    private static async Task SeedSlackChannel(IDatabase db, string teamId, string channelId, int? leagueId, string subscriptions,
        string subscriptionId, int? memberCount)
    {
        var channelKey = $"SlackChannelSub-{teamId}-{channelId}";
        var entries = new List<HashEntry>
        {
            new("teamId", teamId),
            new("channelId", channelId),
            new("subscriptions", subscriptions)
        };
        if (leagueId is { } id)
        {
            entries.Add(new HashEntry("leagueId", id.ToString()));
        }

        await db.HashSetAsync(channelKey, [.. entries]);
        await SeedSubscriptionId(db, "slack", channelKey, teamId, channelId, subscriptionId);
        await db.SetAddAsync($"SlackChannelSubIndex-{teamId}", channelId);
        await SeedEventIndex(db, "SlackEventIndex", teamId, channelId, subscriptions);

        if (memberCount is { } count)
        {
            await db.StringSetAsync($"SlackChannelMemberCount-{channelId}", count);
            await db.StringSetAsync($"SlackChannelMemberCountUpdatedAt-{channelId}", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await db.SetAddAsync("SlackChannelMemberCountIndex", channelId);
        }
    }

    private static async Task SeedDiscord(IDatabase db)
    {
        // Spread of guild sizes/types/staleness so the Discord reach stat, Top 10 table,
        // sort-by-size, and community/private badge on the admin dashboard have something to
        // play with. approximateMemberCount: null means "never swept" (contributes 0, shows "—").
        // updatedAgo: null means "just swept" (fresh); a TimeSpan simulates an overdue sweep, and
        // a very long one simulates the "bot was kicked, count never got cleaned up" scenario
        // discussed for the removed GuildStatusChecker.
        await SeedDiscordGuild(db, "111222333444555666", "DevSeededServer", "999888777666555444", 12345,
            "5eedcafe000000111222333444555666", "5eedbeef000000000000000001234502",
            approximateMemberCount: 240, isCommunity: false, updatedAgo: null);

        await SeedDiscordGuild(db, "1546966580007542937", "fplbotdev-throwaway-discord", "1546966580976549940", 12345,
            "5eedcafe000001546966580007542937", "5eedbeef000000000000000001234503",
            approximateMemberCount: 31, isCommunity: false, updatedAgo: null);

        await SeedDiscordGuild(db, "200300400500600700", "Indie League Chat", "200300400500600701", 54321,
            "5eedcafe000000200300400500600700", "5eedbeef000000000000000002003004",
            approximateMemberCount: 8, isCommunity: false, updatedAgo: TimeSpan.FromMinutes(20));

        await SeedDiscordGuild(db, "300400500600700800", "University FPL Society", "300400500600700801", 65432,
            "5eedcafe000000300400500600700800", "5eedbeef000000000000000003004005",
            approximateMemberCount: 312, isCommunity: false, updatedAgo: TimeSpan.FromHours(3));

        await SeedDiscordGuild(db, "400500600700800900", "Reddit FPL Community", "400500600700800901", 76543,
            "5eedcafe000000400500600700800900", "5eedbeef000000000000000004005006",
            approximateMemberCount: 14_500, isCommunity: true, updatedAgo: TimeSpan.FromHours(1));

        await SeedDiscordGuild(db, "500600700800900100", "Massive Public Hub", "500600700800900101", 87654,
            "5eedcafe000000500600700800900100", "5eedbeef000000000000000005006007",
            approximateMemberCount: 89_000, isCommunity: true, updatedAgo: TimeSpan.FromDays(5));

        await SeedDiscordGuild(db, "600700800900100200", "Legacy Kicked Server", "600700800900100201", 98765,
            "5eedcafe000000600700800900100200", "5eedbeef000000000000000006007008",
            approximateMemberCount: 5_200, isCommunity: false, updatedAgo: TimeSpan.FromDays(240));

        // Never swept at all - exercises "servers not yet counted contribute zero" / "—" display.
        await SeedDiscordGuild(db, "700800900100200300", "Freshly Installed Server", "700800900100200301", 11223,
            "5eedcafe000000700800900100200300", "5eedbeef000000000000000007008009",
            approximateMemberCount: null, isCommunity: null, updatedAgo: null);
    }

    // Mirrors GuildMemberCountRepository's key format (Data/Discord/GuildMemberCountRepository.cs):
    // count/updatedAt/isCommunity as separate string keys plus a GuildMemberCountIndex set, so the
    // Discord reach admin endpoints read this back exactly as they would a real nightly sweep.
    private static async Task SeedDiscordGuild(IDatabase db, string guildId, string name, string channelId, int leagueId,
        string installationId, string subscriptionId, int? approximateMemberCount, bool? isCommunity, TimeSpan? updatedAgo)
    {
        var guildKey = $"Guild-{guildId}";
        await db.HashSetAsync(guildKey, [new HashEntry("name", name)]);
        await db.SetAddAsync("GuildIndex", guildId);
        await SeedInstallationId(db, "discord", guildKey, guildId, installationId);

        await db.HashSetAsync($"GuildSubs-{guildId}-Channel-{channelId}", [
            new HashEntry("guildid", guildId),
            new HashEntry("channelid", channelId),
            new HashEntry("leagueid", leagueId.ToString()),
            new HashEntry("subs", AllSubs)
        ]);
        await db.SetAddAsync($"GuildChannelSubIndex-{guildId}", channelId);
        await SeedSubscriptionId(db, "discord", $"GuildSubs-{guildId}-Channel-{channelId}", guildId, channelId, subscriptionId);
        await SeedEventIndex(db, "GuildEventIndex", guildId, channelId, AllSubs);

        if (approximateMemberCount is { } count)
        {
            var updatedAt = DateTimeOffset.UtcNow - (updatedAgo ?? TimeSpan.Zero);
            await db.StringSetAsync($"GuildMemberCount-{guildId}", count);
            await db.StringSetAsync($"GuildMemberCountUpdatedAt-{guildId}", updatedAt.ToUnixTimeMilliseconds());
            await db.StringSetAsync($"GuildMemberCountIsCommunity-{guildId}", isCommunity ?? false);
            await db.SetAddAsync("GuildMemberCountIndex", guildId);
        }

        Console.WriteLine($"[DevSeeder] Inserted Discord guild {guildKey} ({name}, league {leagueId}, members {approximateMemberCount?.ToString() ?? "unswept"}).");
    }

    // Mirrors WebPushSubscriberRepository's key format (Data/Web/WebPushSubscriberRepository.cs):
    // one hash per subscriber plus a WebPushSubIndex sorted set (scored by "created") and a
    // WebPushSubEvent-{event} set per subscribed event, so the admin subscriber list/count and
    // GetPage's newest-first ordering all work against this exactly as they would real data.
    private static async Task SeedWebPushSubscribers(IDatabase db)
    {
        await SeedWebPushSubscriber(db, "5eedf00d0000000000000000000000a1", "Alice", 12345, ["Standings", "Deadlines", "FixtureGoals"], createdAgo: TimeSpan.FromDays(30));
        await SeedWebPushSubscriber(db, "5eedf00d0000000000000000000000b2", "Bo", null, ["Deadlines"], createdAgo: TimeSpan.FromDays(14));
        await SeedWebPushSubscriber(db, "5eedf00d0000000000000000000000c3", "Casper", 23456, ["Standings", "Captains", "Transfers", "InjuryUpdates", "PriceChanges"], createdAgo: TimeSpan.FromDays(6));
        await SeedWebPushSubscriber(db, "5eedf00d0000000000000000000000d4", null, null, ["PriceChanges"], createdAgo: TimeSpan.FromHours(18));
        await SeedWebPushSubscriber(db, "5eedf00d0000000000000000000000e5", "Erna", 34567, ["Standings", "Deadlines"], createdAgo: TimeSpan.FromHours(2));

        Console.WriteLine("[DevSeeder] Inserted 5 Web Push subscribers.");
    }

    private static async Task SeedWebPushSubscriber(IDatabase db, string id, string? name, int? leagueId, string[] events, TimeSpan createdAgo)
    {
        var key = $"WebPushSub-{id}";
        var entries = new List<HashEntry>
        {
            new("id", id),
            new("endpoint", $"https://push.example.dev/{id}"),
            new("p256dh", "BDevFakeP256dhKeyForLocalSeedDataOnlyNotReal"),
            new("auth", "DevFakeAuthSecretForLocalSeedData"),
            new("subs", string.Join(" ", events))
        };
        if (name is not null) entries.Add(new HashEntry("name", name));
        if (leagueId is { } id_) entries.Add(new HashEntry("leagueid", id_));

        var createdAt = DateTimeOffset.UtcNow - createdAgo;
        entries.Add(new HashEntry("created", createdAt.ToUnixTimeMilliseconds()));

        await db.HashSetAsync(key, [.. entries]);
        await db.SortedSetAddAsync("WebPushSubIndex", id, createdAt.ToUnixTimeMilliseconds());
        foreach (var fplEvent in events)
        {
            await db.SetAddAsync($"WebPushSubEvent-{fplEvent}", id);
        }
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
