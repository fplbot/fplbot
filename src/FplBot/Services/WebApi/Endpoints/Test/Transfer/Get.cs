using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Test.Transfer;

public static class TestTransfer
{
    public static async Task<IResult> Get(IWebHostEnvironment env, IPublishEndpoint publishEndpoint)
    {
        if (env.IsProduction())
            return TypedResults.Unauthorized();

        var transfer = new InternalPremiershipTransfer("Dorkiolo", "AVL", "CHE");
        var transfers = new List<InternalPremiershipTransfer>() { transfer };
        var transferredEvent = new PremiershipPlayerTransferred(transfers);
        await publishEndpoint.Publish(transferredEvent);
        return TypedResults.Accepted("", transfer);
    }
}
