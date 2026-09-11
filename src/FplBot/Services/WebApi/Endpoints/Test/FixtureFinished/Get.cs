using MassTransit;

namespace FplBot.WebApi.Endpoints.Test.FixtureFinished;

public static class TestFixtureFinished
{
    public static async Task<IResult> Get(IWebHostEnvironment env, IPublishEndpoint publishEndpoint, int fixtureId = 1)
    {
        if (env.IsProduction())
            return TypedResults.Unauthorized();

        // Fully qualified: this namespace (...Test.FixtureFinished) shares its last segment
        // with the event record, so "FixtureFinished" alone would resolve to the namespace.
        var message = new Messaging.Contracts.Events.v1.FixtureFinished(fixtureId);
        await publishEndpoint.Publish(message);
        return TypedResults.Accepted("", message);
    }
}
