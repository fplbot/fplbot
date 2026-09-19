namespace Fpl.EventPublishers.States;

public interface IFixtureState
{
    Task Reset(int gameweek);
    Task Refresh(int gameweek);
}
