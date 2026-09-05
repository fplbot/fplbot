using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class AppUninstaller : IUninstall
{
    private readonly IPublishEndpoint _publishEndpoint;

    public AppUninstaller(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task OnUninstalled(string teamId, string teamName)
    {
        await _publishEndpoint.Publish(new AppUninstalled(teamId.ToUpper(), teamName));
    }
}
