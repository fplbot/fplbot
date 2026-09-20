using FplBot.Data.Web;
using FplBot.Domain;
using FplBot.Integrations.WebPush;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Web;

public class PublishToWebPushHandler(
    IWebPushSubscriberRepository repo,
    IWebPushSender sender,
    IHostEnvironment env,
    ILogger<PublishToWebPushHandler> logger)
    : IConsumer<PublishToWebPushSubscriber>
{
    public async Task Consume(ConsumeContext<PublishToWebPushSubscriber> context)
    {
        var message = context.Message;
        var subscriberId = new WebPushSubscriberId(message.SubscriberId);
        if (await repo.Find(subscriberId) is not { } subscriber)
        {
            await repo.Delete(subscriberId);
            return;
        }

        var payload = new WebPushPayload(message.Title, message.Body,
            message.LeagueId is { } leagueId ? $"/leagues/{leagueId}" : null);

        if (env.IsDevelopment() && sender is WebPushSender)
        {
            logger.LogInformation("[DEV] Web push to {SubscriberId}: {Title} — {Body}",
                message.SubscriberId, payload.Title, payload.Body);
            return;
        }

        if (await sender.Send(subscriber.PushKeys, payload) == WebPushOutcome.Gone)
        {
            logger.LogInformation("Push endpoint gone, deleting subscriber {SubscriberId}", message.SubscriberId);
            await repo.Delete(subscriberId);
        }
    }
}
