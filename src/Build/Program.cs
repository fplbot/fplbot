using Bullseye;
using SimpleExec;

const string TestApp  = "blank-fplbot-test";
const string ProdApp  = "blank-fplbot";

var version     = Env("VERSION",     "1.0.0-local");
var infoVersion = Env("INFOVERSION", version);

var targets = new Targets();

targets.Add("test",
    "Run all tests (self-contained — no external Redis or ASB required)",
    async () => await Command.RunAsync("dotnet",
        """test src --logger "GitHubActions;report-warnings=false" """));

targets.Add("docker-build-test",
    "Build the Docker image and tag it for all test app process types",
    async () => await BuildImage($"registry.heroku.com/{TestApp}"));

targets.Add("docker-build-prod",
    "Build the Docker image and tag it for all prod app process types",
    async () => await BuildImage($"registry.heroku.com/{ProdApp}"));

targets.Add("docker-push-test",
    "Push all process-type tags to the Heroku test registry (requires HEROKU_TOKEN)",
    async () => await PushImages($"registry.heroku.com/{TestApp}"));

targets.Add("docker-push-prod",
    "Push all process-type tags to the Heroku prod registry (requires HEROKU_TOKEN)",
    async () => await PushImages($"registry.heroku.com/{ProdApp}"));

targets.Add("deploy-test",
    "Release containers to the test Heroku app (requires HEROKU_API_KEY)",
    async () => await Command.RunAsync("heroku",
        $"container:release web eventpublisher indexer eventhandler --app {TestApp}"));

targets.Add("deploy-prod",
    "Release containers to the prod Heroku app (requires HEROKU_API_KEY)",
    async () => await Command.RunAsync("heroku",
        $"container:release web eventpublisher indexer eventhandler --app {ProdApp}"));

await targets.RunAndExitAsync(args);

async Task BuildImage(string registry)
{
    var buildArgs = $"--build-arg INFOVERSION={infoVersion} --build-arg VERSION={version} -f ./src/Dockerfile ./src";
    var baseTag = "fplbot-runtime:current";
    await Command.RunAsync("docker", $"build -t {baseTag} {buildArgs}");

    foreach (var (processType, serviceName) in ProcessServices())
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, $"FROM {baseTag}\nCMD [\"--services\", \"{serviceName}\"]");
        await Command.RunAsync("docker", $"build -t {registry}/{processType} -f {tmp} .");
        File.Delete(tmp);
    }
}

async Task PushImages(string registry)
{
    foreach (var processType in ProcessServices().Keys)
        await Command.RunAsync("docker", $"push {registry}/{processType}");
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
