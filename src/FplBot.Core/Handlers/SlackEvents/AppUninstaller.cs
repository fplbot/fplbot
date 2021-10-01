using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;

namespace FplBot.Core.Handlers
{
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
}
