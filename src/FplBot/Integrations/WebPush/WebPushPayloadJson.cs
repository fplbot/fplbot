using System.Text.Json;

namespace FplBot.Integrations.WebPush;

public static class WebPushPayloadJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(WebPushPayload payload) => JsonSerializer.Serialize(payload, Options);
}
