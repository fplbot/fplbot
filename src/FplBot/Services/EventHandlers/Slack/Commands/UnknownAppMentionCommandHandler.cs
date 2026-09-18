using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack.Commands;

public class UnknownAppMentionCommandHandler : IConsumer<ProcessUnknownAppMention>
{
    public async Task Consume(ConsumeContext<ProcessUnknownAppMention> context)
    {
        var command = context.Message;
        await context.Publish(new UnknownAppMentionReceived(command.TeamId, command.User, command.Text));
        await context.Publish(new PublishSlackThreadMessage(command.TeamId, command.ChannelId, command.Ts,
            "🤷‍♀️ Ok, that clearly did not work. Maybe try the `help` command to see my available commands?"));
    }
}
