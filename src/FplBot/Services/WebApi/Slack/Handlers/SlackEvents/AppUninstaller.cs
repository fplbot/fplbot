using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class AppUninstaller : IUninstall
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AppUninstaller(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task OnUninstalled(string teamId, string teamName)
    {
        using var scope = _scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publisher.Publish(new AppUninstalled(teamId.ToUpper(), teamName));
    }
}
