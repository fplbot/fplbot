using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;

namespace FplBot.WebApi.Endpoints.Test.RemovedFixtures;

public static class TestRemovedFixture
{
    public static async Task<IActionResult> Get(IWebHostEnvironment env, IMessageSession session)
    {
        if (env.IsProduction())
            return new UnauthorizedResult();

        var removedFixture = new RemovedFixture(1, new(1, "Arsenal", "ARS"), new(2, "Chelsea", "CHE"));
        var message = new FixtureRemovedFromGameweek(1337, removedFixture);
        await session.Publish(message);
        return new AcceptedResult("", message);
    }
}
