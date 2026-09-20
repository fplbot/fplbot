using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Bullseye;
using SimpleExec;
using StackExchange.Redis;

const string TestApp = "blank-fplbot-test";
const string ProdApp = "blank-fplbot";

var version = Env("VERSION", "1.0.0-local");
var infoVersion = Env("INFOVERSION", version);

var targets = new Targets();

targets.Add("test",
    "Run all tests",
    async () => await Command.RunAsync("dotnet",
        "test src -p:TreatWarningsAsErrors=true --report-gh"));

targets.Add("client-build",
    "Install dependencies and build the WebApi ClientApp",
    async () => await BuildClientApp());

targets.Add("ci",
    "Run tests and build the client app (used by CI)",
    ["test", "client-build"]);

targets.Add("docker-build",
    "Build the Docker image once and tag it locally for all process types",
    async () => await BuildImage());

targets.Add("docker-build-from-local",
    "Like docker-build, but for building on a local machine whose Docker defaults Heroku's registry rejects (e.g. Apple Silicon + Docker Desktop's containerd image store) - pins linux/amd64 and disables provenance/SBOM attestation manifests",
    async () => await BuildImage("--platform linux/amd64 --provenance=false --sbom=false"));

targets.Add("docker-push-test",
    "Retag and push local images to the Heroku test registry (requires HEROKU_TOKEN)",
    async () => await PushImages($"registry.heroku.com/{TestApp}"));

targets.Add("docker-push-prod",
    "Retag and push local images to the Heroku prod registry (requires HEROKU_TOKEN)",
    async () => await PushImages($"registry.heroku.com/{ProdApp}"));

targets.Add("deploy-test",
    "Release containers to the test Heroku app (requires HEROKU_API_KEY)",
    async () => await Command.RunAsync("heroku",
        $"container:release web eventpublisher indexer eventhandler --app {TestApp}"));

targets.Add("deploy-prod",
    "Release containers to the prod Heroku app (requires HEROKU_API_KEY)",
    async () => await Command.RunAsync("heroku",
        $"container:release web eventpublisher indexer eventhandler --app {ProdApp}"));

targets.Add("backup-redis-test",
    "Dump Slack/Discord installation Redis data from the test app to a local JSON file (read-only)",
    async () => await BackupInstallations(TestApp));

targets.Add("backup-redis-prod",
    "Dump Slack/Discord installation Redis data from prod to a local JSON file (read-only)",
    async () => await BackupInstallations(ProdApp));

targets.Add("backfill-slack-index-test",
    "Backfill the TeamIndex Redis set on the test app from existing TeamId-* keys (idempotent)",
    async () => await BackfillSlackIndex(TestApp));

targets.Add("backfill-slack-index-prod",
    "Backfill the TeamIndex Redis set on prod from existing TeamId-* keys (idempotent)",
    async () => await BackfillSlackIndex(ProdApp));

targets.Add("backfill-event-index-test",
    "Backfill the per-event GuildEventIndex-*/SlackEventIndex-* sets on the test app from existing channel subscriptions (idempotent)",
    async () => await BackfillEventIndexes(TestApp));

targets.Add("backfill-event-index-prod",
    "Backfill the per-event GuildEventIndex-*/SlackEventIndex-* sets on prod from existing channel subscriptions (idempotent)",
    async () => await BackfillEventIndexes(ProdApp));

targets.Add("backfill-internal-ids-test",
    "Backfill internal installation/subscription ids and their InstallationId-*/SubId-* reverse indexes on the test app (idempotent)",
    async () => await BackfillInternalIds(TestApp));

targets.Add("backfill-internal-ids-prod",
    "Backfill internal installation/subscription ids and their InstallationId-*/SubId-* reverse indexes on prod (idempotent)",
    async () => await BackfillInternalIds(ProdApp));

targets.Add("vapid-keys",
    "Generate a VAPID keypair for web push (one-time; rotating it invalidates every existing subscription)",
    () =>
    {
        var keys = WebPush.VapidHelper.GenerateVapidKeys();
        Console.WriteLine($"WebPush:PublicKey  {keys.PublicKey}");
        Console.WriteLine($"WebPush:PrivateKey {keys.PrivateKey}");
        Console.WriteLine();
        Console.WriteLine("dotnet user-secrets set WebPush:PublicKey \"<public>\" --project src/FplBot");
        Console.WriteLine("dotnet user-secrets set WebPush:PrivateKey \"<private>\" --project src/FplBot");
    });

targets.Add("publish-slash-command-test",
    "Register or update one Discord slash command in one guild of the test app's Discord application (SLASH_COMMAND=<name> GUILD_ID=<id>, requires HEROKU_API_KEY)",
    async () => await PublishSlashCommand(TestApp));

targets.Add("publish-slash-command-prod",
    "Register or update one Discord slash command in one guild of the prod Discord application (SLASH_COMMAND=<name> GUILD_ID=<id>, requires HEROKU_API_KEY)",
    async () => await PublishSlashCommand(ProdApp));

await targets.RunAndExitAsync(args);

async Task BuildImage(string? dockerBuildArgs = null)
{
    await BuildClientApp();
    await PublishBackend();

    var baseTag = "fplbot-runtime:current";
    await Command.RunAsync("docker", $"build {dockerBuildArgs} -t {baseTag} -f ./src/Dockerfile ./src/publish");

    foreach (var (processType, serviceName) in ProcessServices())
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, $"FROM {baseTag}\nCMD [\"--services\", \"{serviceName}\"]");
        await Command.RunAsync("docker", $"build {dockerBuildArgs} -t fplbot/{processType} -f {tmp} .");
        File.Delete(tmp);
    }
}

async Task BuildClientApp()
{
    var clientAppDir = Path.Combine("src", "FplBot", "Services", "WebApi", "ClientApp");
    await Command.RunAsync("npm", "ci", clientAppDir);
    await Command.RunAsync("npm", "run typecheck", clientAppDir);
    await Command.RunAsync("npm", "run build", clientAppDir);
}

async Task PublishBackend()
{
    var publishDir = Path.Combine("src", "publish");
    if (Directory.Exists(publishDir))
        Directory.Delete(publishDir, recursive: true);

    await Command.RunAsync("dotnet",
        $"publish src/FplBot -o {publishDir} -c Release /p:Version={version} /p:InformationalVersion={infoVersion}");
}

async Task PushImages(string registry)
{
    foreach (var processType in ProcessServices().Keys)
    {
        await Command.RunAsync("docker", $"tag fplbot/{processType} {registry}/{processType}");
        await Command.RunAsync("docker", $"push {registry}/{processType}");
    }
}

async Task<string> GetRedisUrl(string app)
{
    var (stdout, _) = await Command.ReadAsync("heroku", $"config:get REDIS_URL --app {app}");
    return stdout.Trim();
}

async Task BackupInstallations(string app)
{
    const int maxConcurrentFetches = 64;

    var redisUrl = await GetRedisUrl(app);
    var redis = await ConnectionMultiplexer.ConnectAsync(ParseRedisUrl(redisUrl));
    var db = redis.GetDatabase();
    var server = redis.GetServers().Single();

    var patterns = new[] { "Guild-*", "GuildSubs-*-Channel-*", "TeamId-*", "SlackChannelSub-*" };
    var keysByPattern = patterns.ToDictionary(p => p, p => server.Keys(pattern: p).ToList());

    var dump = new ConcurrentDictionary<string, Dictionary<string, string>>();
    await Parallel.ForEachAsync(keysByPattern.Values.SelectMany(k => k), new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentFetches }, async (key, _) =>
    {
        var hash = await db.HashGetAllAsync(key);
        dump[key.ToString()] = hash.ToDictionary(h => h.Name.ToString(), h => h.Value.ToString());
    });

    var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "fplbot-backups");
    Directory.CreateDirectory(outputDir);
    var outputPath = Path.Combine(outputDir, $"fplbot-backup-{app}-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
    await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(dump.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value), new JsonSerializerOptions { WriteIndented = true }));

    var counts = string.Join(", ", keysByPattern.Select(kv => $"{kv.Key}={kv.Value.Count}"));
    Console.WriteLine($"Backed up {counts} to {outputPath}");
}

async Task BackfillSlackIndex(string app)
{
    const int maxConcurrentFetches = 64;

    var redisUrl = await GetRedisUrl(app);
    var redis = await ConnectionMultiplexer.ConnectAsync(ParseRedisUrl(redisUrl));
    var db = redis.GetDatabase();
    var server = redis.GetServers().Single();

    var teamKeys = server.Keys(pattern: "TeamId-*").ToList();
    var indexed = 0;
    await Parallel.ForEachAsync(teamKeys, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentFetches }, async (key, _) =>
    {
        var teamId = await db.HashGetAsync(key, "teamId");
        if (!teamId.HasValue) return;
        await db.SetAddAsync("TeamIndex", teamId);
        Interlocked.Increment(ref indexed);
    });

    Console.WriteLine($"Backfilled TeamIndex ({indexed} team(s)) on {app}");
}

async Task BackfillEventIndexes(string app)
{
    // Mirrors FplBot.Domain.FplEvent minus the "All" sentinel - see ExpandBackfillEvents. Build.csproj
    // deliberately has no reference to FplBot's domain types (this whole file talks to Redis in raw
    // strings), so the event names are duplicated here; this is one-off migration tooling meant to be
    // deleted once the backfill has run against both apps (see backup-redis-{test,prod} for precedent).
    string[] concreteFplEvents =
    [
        "Standings", "Captains", "Transfers", "FixtureGoals", "FixtureAssists", "FixtureCards",
        "FixturePenaltyMisses", "FixtureFullTime", "Taunts", "PriceChanges", "InjuryUpdates",
        "Deadlines", "Lineups", "NewPlayers", "FixtureRemovedFromGameweek"
    ];

    IEnumerable<string> ExpandBackfillEvents(string? subscriptionString)
    {
        if (string.IsNullOrWhiteSpace(subscriptionString)) return [];
        var tokens = subscriptionString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Contains("All") ? concreteFplEvents : tokens;
    }

    const int maxConcurrentFetches = 64;
    var redisUrl = await GetRedisUrl(app);
    var redis = await ConnectionMultiplexer.ConnectAsync(ParseRedisUrl(redisUrl));
    var db = redis.GetDatabase();

    var guildIds = await db.SetMembersAsync("GuildIndex");
    var discordIndexed = 0;
    await Parallel.ForEachAsync(guildIds, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentFetches }, async (guildIdValue, _) =>
    {
        var guildId = guildIdValue.ToString();
        var channelIds = await db.SetMembersAsync($"GuildChannelSubIndex-{guildId}");
        foreach (var channelIdValue in channelIds)
        {
            var channelId = channelIdValue.ToString();
            var subscriptions = await db.HashGetAsync($"GuildSubs-{guildId}-Channel-{channelId}", "subs");
            var entry = $"{guildId}:{channelId}";
            foreach (var fplEvent in ExpandBackfillEvents(subscriptions.ToString()))
            {
                await db.SetAddAsync($"GuildEventIndex-{fplEvent}", entry);
            }
            Interlocked.Increment(ref discordIndexed);
        }
    });

    var teamIds = await db.SetMembersAsync("TeamIndex");
    var slackIndexed = 0;
    await Parallel.ForEachAsync(teamIds, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentFetches }, async (teamIdValue, _) =>
    {
        var teamId = teamIdValue.ToString();
        var channelIds = await db.SetMembersAsync($"SlackChannelSubIndex-{teamId}");
        foreach (var channelIdValue in channelIds)
        {
            var channelId = channelIdValue.ToString();
            var subscriptions = await db.HashGetAsync($"SlackChannelSub-{teamId}-{channelId}", "subscriptions");
            var entry = $"{teamId}:{channelId}";
            foreach (var fplEvent in ExpandBackfillEvents(subscriptions.ToString()))
            {
                await db.SetAddAsync($"SlackEventIndex-{fplEvent}", entry);
            }
            Interlocked.Increment(ref slackIndexed);
        }
    });

    Console.WriteLine($"Backfilled event indexes on {app}: {discordIndexed} Discord channel(s), {slackIndexed} Slack channel(s)");
}

async Task BackfillInternalIds(string app)
{
    const int maxConcurrentFetches = 64;

    var redisUrl = await GetRedisUrl(app);
    var redis = await ConnectionMultiplexer.ConnectAsync(ParseRedisUrl(redisUrl));
    var db = redis.GetDatabase();

    var mintedIds = 0;
    var indexed = 0;

    // Keeps an id that is already there, so a rerun mints nothing new and only repairs a missing
    // reverse index entry. NotExists also means a concurrent app read converges on one id.
    async Task<string> EnsureId(string key)
    {
        var stored = await db.HashGetAsync(key, "id");
        if (stored.HasValue) return stored.ToString();

        var candidate = Guid.NewGuid().ToString("N");
        if (await db.HashSetAsync(key, "id", candidate, When.NotExists))
        {
            Interlocked.Increment(ref mintedIds);
            return candidate;
        }

        return (await db.HashGetAsync(key, "id")).ToString();
    }

    async Task BackfillOne(string platform, string installationKey, string externalId, string channelSubIndexKey,
        Func<string, string> toChannelSubKey)
    {
        var installationId = await EnsureId(installationKey);
        await db.StringSetAsync($"InstallationId-{installationId}", $"{platform}:{externalId}");
        Interlocked.Increment(ref indexed);

        foreach (var channelIdValue in await db.SetMembersAsync(channelSubIndexKey))
        {
            var channelId = channelIdValue.ToString();
            var subId = await EnsureId(toChannelSubKey(channelId));
            await db.StringSetAsync($"SubId-{subId}", $"{platform}:{externalId}:{channelId}");
            Interlocked.Increment(ref indexed);
        }
    }

    var guildIds = await db.SetMembersAsync("GuildIndex");
    await Parallel.ForEachAsync(guildIds, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentFetches }, async (guildIdValue, _) =>
    {
        var guildId = guildIdValue.ToString();
        await BackfillOne("discord", $"Guild-{guildId}", guildId, $"GuildChannelSubIndex-{guildId}",
            channelId => $"GuildSubs-{guildId}-Channel-{channelId}");
    });

    var teamIds = await db.SetMembersAsync("TeamIndex");
    await Parallel.ForEachAsync(teamIds, new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentFetches }, async (teamIdValue, _) =>
    {
        var teamId = teamIdValue.ToString();
        await BackfillOne("slack", $"TeamId-{teamId}", teamId, $"SlackChannelSubIndex-{teamId}",
            channelId => $"SlackChannelSub-{teamId}-{channelId}");
    });

    Console.WriteLine($"Backfilled internal ids on {app}: {mintedIds} id(s) minted, {indexed} reverse index entrie(s) written");
}

async Task PublishSlashCommand(string app)
{
    var name = Env("SLASH_COMMAND", "");
    var commands = SlashCommands();
    if (!commands.TryGetValue(name, out var command))
    {
        throw new Exception($"Set SLASH_COMMAND to one of: {string.Join(", ", commands.Keys)}");
    }

    var guild = Env("GUILD_ID", "");
    if (guild.Length == 0)
    {
        throw new Exception("Set GUILD_ID to the guild to publish the command to");
    }

    var (appId, _) = await Command.ReadAsync("heroku", $"config:get DiscordAppId --app {app}");
    var (token, _) = await Command.ReadAsync("heroku", $"config:get DISCORD_TOKEN --app {app}");

    using var http = new HttpClient();
    http.DefaultRequestHeaders.Add("Authorization", $"Bot {token.Trim()}");
    var response = await http.PostAsync($"https://discord.com/api/v10/applications/{appId.Trim()}/guilds/{guild}/commands",
        new StringContent(JsonSerializer.Serialize(command), Encoding.UTF8, "application/json"));
    var responseBody = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Discord rejected /{name} ({(int)response.StatusCode}): {responseBody}");
    }

    Console.WriteLine($"Published /{name} to guild {guild} of {app}'s Discord application.");
}

Dictionary<string, object> SlashCommands()
{
    // Mirrors FplBot.Discord.DiscordSlashCommandsEnsurer.GetDefinedCommands() and, for the event
    // choices, FplBot.Data.EventSubscription. Build.csproj deliberately has no reference to FplBot
    // (see BackfillEventIndexes for the same tradeoff), so a command whose name, description or
    // options change has to be updated in both places.
    string[] eventSubscriptions =
    [
        "All", "Standings", "Captains", "Transfers", "FixtureGoals", "FixtureAssists", "FixtureCards",
        "FixturePenaltyMisses", "FixtureFullTime", "Taunts", "PriceChanges", "InjuryUpdates",
        "Deadlines", "Lineups", "NewPlayers", "FixtureRemovedFromGameweek"
    ];

    object EventChoices() => new
    {
        type = 3,
        name = "event",
        description = "Available events",
        required = true,
        choices = eventSubscriptions.Select(e => new { name = e, value = e })
    };

    return new Dictionary<string, object>
    {
        ["help"] = new { name = "help", description = "Shows help" },
        ["follow"] = new
        {
            name = "follow",
            description = "Follow a FPL league in this channel",
            options = new object[]
            {
                new { type = 4, name = "leagueid", description = "A FPL League Id.", required = true }
            }
        },
        ["standings"] = new { name = "standings", description = "Post the standings for the league this channel follows" },
        ["subscriptions"] = new
        {
            name = "subscriptions",
            description = "Manage subscription",
            options = new object[]
            {
                new { type = 1, name = "add", description = "add/remove", options = new[] { EventChoices() } },
                new { type = 1, name = "remove", description = "add/remove", options = new[] { EventChoices() } }
            }
        }
    };
}

ConfigurationOptions ParseRedisUrl(string redisUrl)
{
    var uri = new Uri(redisUrl);
    var userInfo = uri.UserInfo.Split(':');
    var options = new ConfigurationOptions
    {
        Password = userInfo.Length > 1 ? userInfo[1] : null,
        EndPoints = { uri.Host + ":" + uri.Port },
        Ssl = redisUrl.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase),
        SslClientAuthenticationOptions = _ => new System.Net.Security.SslClientAuthenticationOptions
        {
            TargetHost = uri.Host,
            RemoteCertificateValidationCallback = (_, _, _, _) => true,
        }
    };
    if (!string.IsNullOrEmpty(userInfo[0]))
        options.User = userInfo[0];
    return options;
}

Dictionary<string, string> ProcessServices() => new()
{
    ["web"] = "WebApi",
    ["eventpublisher"] = "EventPublishers",
    ["eventhandler"] = "EventHandlers",
    ["indexer"] = "SearchIndexer",
};

static string Env(string name, string fallback) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;
