using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack;

public class PublishToSlackHandler : IHandleMessages<PublishToSlack>, IHandleMessages<PublishSlackThreadMessage>
{
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly IHostEnvironment _env;

    public PublishToSlackHandler(ISlackWorkSpacePublisher publisher, IHostEnvironment env)
    {
        _publisher = publisher;
        _env = env;
    }

    public async Task Handle(PublishToSlack publish, IMessageHandlerContext context)
    {
        var publishMessage = publish.Message;
        if (_env.IsDevelopment())
        {
            publishMessage = $"[{Environment.MachineName}]\n{publishMessage}";
        }

        await _publisher.PublishToWorkspace(publish.TeamId, new ChatPostMessageRequest { Channel = publish.Channel, Text = publishMessage, unfurl_links = "false"});
    }

    public async Task Handle(PublishSlackThreadMessage message, IMessageHandlerContext context)
    {
        var publishMessage = message.Message;
        if (_env.IsDevelopment())
        {
            publishMessage = $"[{Environment.MachineName}]\n{publishMessage}";
        }
        await _publisher.PublishToWorkspace(message.TeamId, new ChatPostMessageRequest
        {
            Channel = message.Channel, thread_ts = message.Timestamp, Text = publishMessage, unfurl_links = "false"
        });
    }
}
