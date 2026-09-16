using Discord.Net.HttpClients;

namespace FplBot.EventHandlers;

public static class DeliveryFailureClassifier
{
    private static readonly HashSet<string> SlackChannelScopedErrors =
    [
        "channel_not_found",
        "is_archived",
        "not_in_channel"
    ];

    private static readonly HashSet<int> DiscordChannelScopedCodes =
    [
        10003,
        50001,
        50013
    ];

    public static string? Classify(string slackError) =>
        SlackChannelScopedErrors.Contains(slackError) ? slackError : null;

    public static string? Classify(DiscordApiException e) =>
        e.ErrorCode is { } code && DiscordChannelScopedCodes.Contains(code) ? code.ToString() : null;
}
