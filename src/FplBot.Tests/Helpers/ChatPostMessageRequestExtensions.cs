using System.Text.Json;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Tests.Helpers;

public static class ChatPostMessageRequestExtensions
{
    public static string AllText(this ChatPostMessageRequest msg) =>
        !string.IsNullOrEmpty(msg.Text)
            ? msg.Text
            : string.Join(' ', (msg.Blocks ?? []).Select(b => JsonSerializer.Serialize(b, b.GetType())));
}
