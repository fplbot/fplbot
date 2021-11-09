using Fpl.Client.Abstractions;

namespace FplBot.Slack.Helpers;

internal class GameweekHelper : IGameweekHelper
{
    private readonly IGlobalSettingsClient _gameweekClient;

    public GameweekHelper(IGlobalSettingsClient gameweekClient)
    {
        _gameweekClient = gameweekClient;
    }

    public async Task<int?> ExtractGameweekOrFallbackToCurrent(string messageText, string pattern)
    {
        var extractedGw = MessageHelper.ExtractGameweek(messageText, pattern);
        return extractedGw ?? (await _gameweekClient.GetGlobalSettings()).Gameweeks.SingleOrDefault(x => x.IsCurrent)?.Id;
    }
}

internal interface IGameweekHelper
{
    /// <summary>
    /// Extracts gameweek number from message text using pattern "some text here {gw}". E.g. "captains {gw}". Returns current gameweek if not found in text.
    /// </summary>
    /// <param name="messageText">Message text</param>
    /// <param name="pattern">Pattern (excluding bot handle). E.g. "captains {gw}"</param>
    /// <returns>Extracted gameweek if found, else current gameweek.</returns>
    Task<int?> ExtractGameweekOrFallbackToCurrent(string messageText, string pattern);
}