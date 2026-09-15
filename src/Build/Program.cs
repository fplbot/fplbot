using Bullseye;
using SimpleExec;

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
    async () => await BuildImage());

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

targets.Add("backup-discord-index-test",
    "Dump Discord guild/channel Redis data from the test app to a local JSON file (read-only)",
    async () => await BackupDiscordIndex(TestApp));

targets.Add("backup-discord-index-prod",
    "Dump Discord guild/channel Redis data from prod to a local JSON file (read-only)",
    async () => await BackupDiscordIndex(ProdApp));

targets.Add("backfill-discord-index-test",
    "Backfill GuildIndex/GuildChannelSubIndex-* Redis sets on the test app from existing data (idempotent)",
    async () => await BackfillDiscordIndex(TestApp));

targets.Add("backfill-discord-index-prod",
    "Backfill GuildIndex/GuildChannelSubIndex-* Redis sets on prod from existing data (idempotent)",
    async () => await BackfillDiscordIndex(ProdApp));

await targets.RunAndExitAsync(args);

async Task BuildImage()
{
    await BuildClientApp();
    await PublishBackend();

    var baseTag = "fplbot-runtime:current";
    await Command.RunAsync("docker", $"build -t {baseTag} -f ./src/Dockerfile ./src/publish");

    foreach (var (processType, serviceName) in ProcessServices())
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, $"FROM {baseTag}\nCMD [\"--services\", \"{serviceName}\"]");
        await Command.RunAsync("docker", $"build -t fplbot/{processType} -f {tmp} .");
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

async Task BackupDiscordIndex(string app)
{
    var redisUrl = await GetRedisUrl(app);
    var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "fplbot-backups");
    var outputPath = Path.Combine(outputDir, $"discord-backup-{app}-{DateTime.UtcNow:yyyyMMddHHmmss}.json");

    await Command.RunAsync("dotnet",
        $"run --project src/FplBot -- --backup-discord-channel-index {outputPath}",
        configureEnvironment: env => env["REDIS_URL"] = redisUrl,
        secrets: [redisUrl]);
}

async Task BackfillDiscordIndex(string app)
{
    var redisUrl = await GetRedisUrl(app);

    await Command.RunAsync("dotnet",
        "run --project src/FplBot -- --backfill-discord-channel-index",
        configureEnvironment: env => env["REDIS_URL"] = redisUrl,
        secrets: [redisUrl]);
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
