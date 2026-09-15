using System.Collections.Concurrent;
using System.Text.Json;
using Bullseye;
using SimpleExec;
using StackExchange.Redis;

const string TestApp  = "blank-fplbot-test";
const string ProdApp  = "blank-fplbot";

var version     = Env("VERSION",     "1.0.0-local");
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
    async () => await BuildImage(""));

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

await targets.RunAndExitAsync(args);

async Task BuildImage(string dockerBuildArgs)
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
    ["web"]             = "WebApi",
    ["eventpublisher"]  = "EventPublishers",
    ["eventhandler"]    = "EventHandlers",
    ["indexer"]         = "SearchIndexer",
};

static string Env(string name, string fallback) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;
