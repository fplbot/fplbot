using FplBot.WebApi.Endpoints.Test.Gameweekstart;
using FplBot.WebApi.Endpoints.Test.Goal;
using FplBot.WebApi.Endpoints.Test.RemovedFixtures;
using FplBot.WebApi.Endpoints.Test.Transfer;

namespace FplBot.WebApi.Endpoints.Test;

public static class TestEndpoints
{
    public static void Map(WebApplication app, string baseRoute)
    {
        app.MapGet($"{baseRoute}/transfers", TestTransfer.Get);
        app.MapGet($"{baseRoute}/removedfixture", TestRemovedFixture.Get);
        app.MapGet($"{baseRoute}/goal", TestGoal.Get);
        app.MapGet($"{baseRoute}/gameweekstart", TestGwStart.Get);
    }
}
