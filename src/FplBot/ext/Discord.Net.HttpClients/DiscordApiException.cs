using System.Net;
using System.Text.Json;

namespace Discord.Net.HttpClients;

public class DiscordApiException(HttpStatusCode? statusCode, int? errorCode, string message)
    : HttpRequestException(message, null, statusCode)
{
    public int? ErrorCode { get; } = errorCode;

    public static DiscordApiException From(HttpResponseMessage response, string responseBody)
    {
        int? code = null;
        string? discordMessage = null;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var parsed))
            {
                code = parsed;
            }

            if (document.RootElement.TryGetProperty("message", out var messageElement))
            {
                discordMessage = messageElement.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return new DiscordApiException(response.StatusCode, code,
            $"Discord responded {(int)response.StatusCode} ({code?.ToString() ?? "no code"}): {discordMessage ?? responseBody}");
    }
}
