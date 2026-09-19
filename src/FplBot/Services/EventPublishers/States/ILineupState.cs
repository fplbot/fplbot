namespace Fpl.EventPublishers.States;

public interface ILineupState
{
    Task Reset(int gameweek);
    Task Refresh(int gameweek);
    void LogState();
}
