using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Slack.Handlers.SlackEvents;

public class AppUninstaller : IUninstall
{
    private readonly IMessageSession _messageSession;

    public AppUninstaller(IMessageSession messageSession)
    {
        _messageSession = messageSession;
    }

    public async Task OnUninstalled(string teamId, string teamName)
    {
        await _messageSession.Publish(new AppUninstalled(teamId, teamName));
    }
}
