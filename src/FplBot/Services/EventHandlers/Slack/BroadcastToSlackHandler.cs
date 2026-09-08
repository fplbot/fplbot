using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class BroadcastToSlackHandler(ISlackWorkSpacePublisher publisher, ILogger<BroadcastToSlackHandler> logger) : IConsumer<BroadcastToSlack>
{
    public async Task Consume(ConsumeContext<BroadcastToSlack> context)
    {
        logger.LogInformation("BROADCASTING {Message} TO ALL SLACK WORKSPACES", context.Message.Message);
        await publisher.PublishToAllWorkspaceChannels(context.Message.Message);
    }
}
