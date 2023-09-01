using FplBot.Messaging.Contracts.Commands.v1;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;

namespace FplBot.WebApi.Endpoints.Test.Gameweekstart;

public static class TestGwStart
{
    public static async Task<IActionResult> Get(IWebHostEnvironment env, IMessageSession session)
    {
        if (env.IsProduction())
            return new UnauthorizedResult();

        var options = new SendOptions();
        options.RequireImmediateDispatch();
        options.SetDestination("FplBot.EventHandlers.Discord");
        var cmd = new ProcessGameweekStartedForGuildChannel("893932860162064414", "897565955587186838", 4);
        await session.Send(cmd, options);

        var otheroptions = new SendOptions();
        otheroptions.RequireImmediateDispatch();
        otheroptions.SetDestination("FplBot.EventHandlers.Slack");
        var cmdSlack = new ProcessGameweekStartedForSlackWorkspace("t016b9n3u7p".ToUpper(), 4);
        await session.Send(cmdSlack, otheroptions);
        return new AcceptedResult("", cmd);
    }
}
