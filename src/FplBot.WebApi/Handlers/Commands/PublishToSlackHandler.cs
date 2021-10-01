using System;
using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Slack.Abstractions;
using NServiceBus;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.WebApi.Handlers.Commands
{
    public class PublishToSlackHandler : IHandleMessages<PublishToSlack>, IHandleMessages<PublishSlackThreadMessage>
    {
        private readonly ISlackWorkSpacePublisher _publisher;

        public PublishToSlackHandler(ISlackWorkSpacePublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task Handle(PublishToSlack publish, IMessageHandlerContext context)
        {
            await _publisher.PublishToWorkspace(publish.TeamId, new ChatPostMessageRequest { Channel = publish.Channel, Text = publish.Message, unfurl_links = "false"});
        }

        public async Task Handle(PublishSlackThreadMessage message, IMessageHandlerContext context)
        {
            await _publisher.PublishToWorkspace(message.TeamId, new ChatPostMessageRequest
            {
                Channel = message.Channel, thread_ts = message.Timestamp, Text = message.Message, unfurl_links = "false"
            });
        }
    }
}
