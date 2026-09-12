using System.Text.Json;

namespace FplBot.Tests.Helpers;

public static class DiscordResponseExtensions
{
    public static string EmbedTitle(this string responseJson, int index = 0) =>
        Embed(responseJson, index).GetProperty("title").GetString()!;

    public static string EmbedDescription(this string responseJson, int index = 0) =>
        Embed(responseJson, index).GetProperty("description").GetString()!;

    private static JsonElement Embed(string responseJson, int index) =>
        JsonDocument.Parse(responseJson).RootElement
            .GetProperty("data")
            .GetProperty("embeds")[index];
}
