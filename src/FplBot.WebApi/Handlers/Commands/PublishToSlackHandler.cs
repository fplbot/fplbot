using System;
using System.Threading.Tasks;
using FplBot.Core.Abstractions;
using FplBot.Messaging.Contracts.Commands.v1;
using NServiceBus;

namespace FplBot.WebApi.Handlers.Commands
{
    public class PublishToSlackHandler : IHandleMessages<PublishToSlack>
    {
        private readonly ISlackWorkSpacePublisher _publisher;

        public PublishToSlackHandler(ISlackWorkSpacePublisher publisher)
        {
            _publisher = publisher;
        }
        
        public async Task Handle(PublishToSlack publish, IMessageHandlerContext context)
        {
            await _publisher.PublishToWorkspace(publish.TeamId, publish.Channel, publish.Message);
        }
    }
}