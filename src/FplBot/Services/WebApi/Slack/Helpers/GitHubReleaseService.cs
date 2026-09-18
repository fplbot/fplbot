using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FplBot.Services.WebApi.Slack.Helpers;

public static class GitHubReleaseService
{
    private class Release { public string? Body { get; set; }}

    public static async Task<string> GetReleaseNotes(string majorMinorPatch)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("fplbot", $"{majorMinorPatch}"));
        try
        {
            var requestUri = $"https://api.github.com/repos/fplbot/fplbot/releases/tags/{majorMinorPatch}";

            var json = await httpClient.GetStringAsync(requestUri);
            var res = JsonSerializer.Deserialize<Release>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull});
            var resBody = res?.Body;
            var splitted = resBody?.Split("\n");
            var listed = splitted?.Select(s =>
            {
                if (s.Contains("Merge branch"))
                    return null;
                var shaRemoved = string.Join(" ", s.Split(" ")[1..]);
                var replaced = Regex.Replace(shaRemoved, "#(\\d+)", replacement: "<https://github.com/fplbot/fplbot/pull/$1|$1>");
                return $"{GetEmoji(replaced)} {replaced}";
            }).Where(s => s != null);
            var joined = $"      {string.Join("\n      ", listed ?? [])}";
            var releaseLinks = $"▪️ <https://github.com/fplbot/fplbot/releases/tag/{majorMinorPatch}|Release notes for {majorMinorPatch}>\n" + joined;
            return releaseLinks;
        }
        catch (HttpRequestException)
        {
        }

        return string.Empty;
    }

    private static string GetEmoji(string text)
    {
        var emoji = "";
        if (Regex.IsMatch(text, "^[Bb]ug(fix(es)?)?|^[Hh]otfix(es)?"))
            emoji += "😡";
        if (Regex.IsMatch(text, "[Rr]efactor"))
            emoji += "🔧";
        if (Regex.IsMatch(text, "[Pp]ull/"))
            emoji += "➕";

        if (emoji.Length < 2)
            emoji += GetRandomEmoji();
        return emoji;
    }

    private static readonly IEnumerable<string> _randomPool =
    [
        "🤠","😻", "🙇‍♀️", "👑","💄", "🎉", "✨", "🎩", "♥️", "💥", "🧨", "⚽️", "🚨", "📣", "🥑", "🏂", "🥁", "🎯", "🎳", "🎲", "🎰", "🐢", "💧", "🌈", "🎖"
    ];
    private static string GetRandomEmoji()
    {
        var random = new Random();
        var next = random.Next(0, _randomPool.Count<string>());
        return _randomPool.ToArray()[next];
    }
}
