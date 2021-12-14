// Slack / Discord handling are 2 endpoints so they don't use the same queue:
// - Don't want one to be prioritized over the other

IHost discordHost = Host.CreateDefaultBuilder(args)
    .CreateDiscordEndpoint()
    .Build();

IHost slackHost = Host.CreateDefaultBuilder(args)
    .CreateSlackEndpoint().Build();

var discordTask = discordHost.RunAsync();
var slackTask = slackHost.RunAsync();
await Task.WhenAll(discordTask, slackTask);
