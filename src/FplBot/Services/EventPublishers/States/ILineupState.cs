namespace Fpl.EventPublishers.States;

internal interface ILineupState
{
    Task Reset(int gameweek);
    Task Refresh(int gameweek);
    void LogState();
}
