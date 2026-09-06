using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class AppUninstaller(IPublishEndpoint publisher) : IUninstall
{
    public async Task OnUninstalled(string teamId, string teamName)
    {
        await publisher.Publish(new AppUninstalled(teamId.ToUpper(), teamName));
    }
}
