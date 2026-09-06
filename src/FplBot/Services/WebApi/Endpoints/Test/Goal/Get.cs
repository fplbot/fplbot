using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Test.Goal;

public static class TestGoal
{
    public static async Task<IResult> Get(IWebHostEnvironment env, IPublishEndpoint publishEndpoint, bool removed = false)
    {
        if (env.IsProduction())
            return TypedResults.Unauthorized();

        var message = FixtureEvents(StatType.GoalsScored, removed);
        await publishEndpoint.Publish(message);
        return TypedResults.Accepted("", message);
    }

    private static FixtureEventsOccured FixtureEvents(StatType type, bool isRemoved)
    {
        List<FixtureEvents> fixtureEventsList = new();
        FixtureTeam home = new(1, "HOM", "HomeTeam");
        FixtureTeam away = new(2, "AWA", "Away");
        FixtureScore fixtureScore = new(home, away, 35, 0, 1);
        Dictionary<StatType, List<PlayerEvent>> statMap = new();
        PlayerDetails playerDetails1 = new(1, "Testerson");
        PlayerDetails playerDetails2 = new(2, Environment.MachineName);
        var teamDetails = TeamType.Home;

        List<PlayerEvent> playerEvents = new()
        {
            new PlayerEvent(playerDetails1, teamDetails, IsRemoved: isRemoved),
            new PlayerEvent(playerDetails2, teamDetails, false),
        };

        statMap.Add(type, playerEvents);

        fixtureEventsList.Add(new FixtureEvents(fixtureScore, statMap));
        return new FixtureEventsOccured(fixtureEventsList);
    }
}
