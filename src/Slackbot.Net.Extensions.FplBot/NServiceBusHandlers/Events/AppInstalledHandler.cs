using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.Extensions.FplBot.Abstractions;

namespace Slackbot.Net.Extensions.FplBot.NServiceBusHandlers.Events
{
    public class AppInstalledHandler : IHandleMessages<AppInstalled>
    {
        private readonly ISlackWorkSpacePublisher _publisher;

        public AppInstalledHandler(ISlackWorkSpacePublisher publisher)
        {
            _publisher = publisher;
        }
        
        public async Task Handle(AppInstalled message, IMessageHandlerContext context)
        {
            await _publisher.PublishToWorkspace("T0A9QSU83", "#fpltest", message.ToString());
        }
    }
}