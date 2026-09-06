using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class AppUninstaller : IUninstall
{
    private readonly IPublishEndpoint _publisher;

    public AppUninstaller(IPublishEndpoint publisher)
    {
        _publisher = publisher;
    }

    public async Task OnUninstalled(string teamId, string teamName)
    {
        await _publisher.Publish(new AppUninstalled(teamId.ToUpper(), teamName));
    }
}
