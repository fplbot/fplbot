namespace Fpl.EventPublishers.States;

internal interface IFixtureState
{
    Task Reset(int gameweek);
    Task Refresh(int gameweek);
}
