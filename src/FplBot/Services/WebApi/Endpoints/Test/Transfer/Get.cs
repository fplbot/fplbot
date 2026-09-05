using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace FplBot.WebApi.Endpoints.Test.Transfer;

public static class TestTransfer
{
    public static async Task<IActionResult> Get(IWebHostEnvironment env, IPublishEndpoint publishEndpoint)
    {
        if (env.IsProduction())
            return new UnauthorizedResult();

        var transfer = new InternalPremiershipTransfer("Dorkiolo", "AVL", "CHE");
        var transfers = new List<InternalPremiershipTransfer>() { transfer };
        var transferredEvent = new PremiershipPlayerTransferred(transfers);
        await publishEndpoint.Publish(transferredEvent);
        return new AcceptedResult("", transfer);
    }
}
