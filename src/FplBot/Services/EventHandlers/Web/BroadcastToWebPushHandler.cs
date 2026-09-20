using FplBot.Data.Web;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Web;

public class BroadcastToWebPushHandler(IWebPushSubscriberRepository repo) : IConsumer<BroadcastToWebPush>
{
    public async Task Consume(ConsumeContext<BroadcastToWebPush> context)
    {
        var message = context.Message;
        foreach (var id in await repo.GetSubscribedTo(FplEvents.SupportedOnWeb))
        {
            await context.Publish(new PublishToWebPushSubscriber(id.Value, message.Title, message.Body, null));
        }
    }
}
