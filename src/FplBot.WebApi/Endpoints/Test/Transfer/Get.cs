using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;

namespace FplBot.WebApi.Endpoints.Test.Transfer;

public static class TestTransfer
{
    public static async Task<IActionResult> Get(IWebHostEnvironment env, IMessageSession session)
    {
        if (env.IsProduction())
            return new UnauthorizedResult();

        var transfer = new InternalPremiershipTransfer("Dorkiolo", "AVL", "CHE");
        var transfers = new List<InternalPremiershipTransfer>() { transfer };
        var transferredEvent = new PremiershipPlayerTransferred(transfers);
        await session.Publish(transferredEvent);
        return new AcceptedResult("", transfer);
    }
}
