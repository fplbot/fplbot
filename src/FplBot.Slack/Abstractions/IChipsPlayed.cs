namespace FplBot.Slack.Abstractions;

internal interface IChipsPlayed
{
    Task<bool> GetHasUsedTripleCaptainForGameWeek(int gameweek, int teamCode);
}