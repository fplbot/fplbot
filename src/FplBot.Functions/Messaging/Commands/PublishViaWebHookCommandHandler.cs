using FplBot.Functions.Messaging.Internal;
using Microsoft.Extensions.Configuration;
using NServiceBus;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Functions.Messaging.Commands;

public class PublishViaWebHookCommandHandler : IHandleMessages<PublishViaWebHook>
{
    private readonly ISlackClient _client;
    private readonly string _prefix;

    public PublishViaWebHookCommandHandler(IConfiguration config, ISlackClientBuilder builder)
    {
        var token = config.GetValue<string>("SlackToken_FplBot_Workspace");
        var env = config.GetValue<string>("DOTNET_ENVIRONMENT");
        _prefix = env == "Production" ? "" : $"{env}: ";
        _client = builder.Build(token);
    }

    public async Task Handle(PublishViaWebHook publish, IMessageHandlerContext context)
    {
        await _client.ChatPostMessage("#fplbot-notifications", $"{_prefix}{publish.Message}");
    }
}
