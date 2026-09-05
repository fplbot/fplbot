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
    "Build all four Docker images tagged for the test Heroku app",
    async () => await BuildImages($"registry.heroku.com/{TestApp}"));

targets.Add("docker-build-prod",
    "Build all four Docker images tagged for the prod Heroku app",
    async () => await BuildImages($"registry.heroku.com/{ProdApp}"));

targets.Add("docker-push-test",
    "Push images to the Heroku test registry (requires HEROKU_TOKEN)",
    async () => await PushImages($"registry.heroku.com/{TestApp}"));

targets.Add("docker-push-prod",
    "Push images to the Heroku prod registry (requires HEROKU_TOKEN)",
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

async Task BuildImages(string registry)
{
    var buildArgs = $"--build-arg INFOVERSION={infoVersion} --build-arg VERSION={version} -f ./src/Dockerfile ./src";
    foreach (var (stage, service) in Services())
        await Command.RunAsync("docker", $"build --target {stage} -t {registry}/{service} {buildArgs}");
}

async Task PushImages(string registry)
{
    foreach (var (_, service) in Services())
        await Command.RunAsync("docker", $"push {registry}/{service}");
}

(string stage, string service)[] Services() =>
[
    ("web",          "web"),
    ("eventpublisher","eventpublisher"),
    ("indexer",      "indexer"),
    ("eventhandler", "eventhandler"),
];

static string Env(string name, string fallback) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;
