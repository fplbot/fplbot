using System.Net;
using FplBot.Domain;
using Microsoft.Extensions.Options;
using WebPush;

namespace FplBot.Integrations.WebPush;

public class WebPushSender(IOptions<WebPushOptions> options, ILogger<WebPushSender> logger) : IWebPushSender
{
    private readonly WebPushClient _client = new();

    public async Task<WebPushOutcome> Send(PushKeys keys, WebPushPayload payload)
    {
        try
        {
            var subscription = new PushSubscription(keys.Endpoint, keys.P256dh, keys.Auth);
            var vapid = new VapidDetails(options.Value.Subject, options.Value.PublicKey, options.Value.PrivateKey);
            await _client.SendNotificationAsync(subscription, WebPushPayloadJson.Serialize(payload), vapid);
            return WebPushOutcome.Delivered;
        }
        catch (WebPushException e) when (e.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            return WebPushOutcome.Gone;
        }
        catch (Exception e)
        {
            var host = Uri.TryCreate(keys?.Endpoint, UriKind.Absolute, out var uri) ? uri.Host : "unknown";
            logger.LogWarning(e, "Web push delivery failed for endpoint host {Host}", host);
            return WebPushOutcome.Failed;
        }
    }
}
