using System.Net;
using System.Text.Json;
using FplBot.Domain;
using Microsoft.Extensions.Options;
using WebPush;

namespace FplBot.Integrations.WebPush;

public class WebPushSender(IOptions<WebPushOptions> options, ILogger<WebPushSender> logger) : IWebPushSender
{
    private readonly WebPushClient _client = new();

    public async Task<WebPushOutcome> Send(PushKeys keys, WebPushPayload payload)
    {
        var subscription = new PushSubscription(keys.Endpoint, keys.P256dh, keys.Auth);
        var vapid = new VapidDetails(options.Value.Subject, options.Value.PublicKey, options.Value.PrivateKey);

        try
        {
            await _client.SendNotificationAsync(subscription, JsonSerializer.Serialize(payload), vapid);
            return WebPushOutcome.Delivered;
        }
        catch (WebPushException e) when (e.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            return WebPushOutcome.Gone;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Web push delivery failed for endpoint host {Host}", new Uri(keys.Endpoint).Host);
            return WebPushOutcome.Failed;
        }
    }
}
