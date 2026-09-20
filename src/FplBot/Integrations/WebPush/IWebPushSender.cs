using FplBot.Domain;

namespace FplBot.Integrations.WebPush;

public enum WebPushOutcome
{
    Delivered,
    Gone,
    Failed
}

public record WebPushPayload(string Title, string Body, string? Link);

public interface IWebPushSender
{
    Task<WebPushOutcome> Send(PushKeys keys, WebPushPayload payload);
}
