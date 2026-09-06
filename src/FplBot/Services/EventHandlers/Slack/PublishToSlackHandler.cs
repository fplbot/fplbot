using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack;

public class PublishToSlackHandler : IConsumer<PublishToSlack>, IConsumer<PublishSlackThreadMessage>
{
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly IHostEnvironment _env;

    public PublishToSlackHandler(ISlackWorkSpacePublisher publisher, IHostEnvironment env)
    {
        _publisher = publisher;
        _env = env;
    }

    public async Task Consume(ConsumeContext<PublishToSlack> context)
    {
        var publish = context.Message;
        var publishMessage = publish.Message;
        if (_env.IsDevelopment())
        {
            publishMessage = $"[{Environment.MachineName}]\n{publishMessage}";
        }

        await _publisher.PublishToWorkspace(publish.TeamId, new ChatPostMessageRequest { Channel = publish.Channel, Text = publishMessage, unfurl_links = "false"});
    }

    public async Task Consume(ConsumeContext<PublishSlackThreadMessage> context)
    {
        var message = context.Message;
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
