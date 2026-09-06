using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Test.Gameweekstart;

public static class TestGwStart
{
    public static async Task<IResult> Get(IWebHostEnvironment env, ISendEndpointProvider sendEndpointProvider)
    {
        if (env.IsProduction())
            return TypedResults.Unauthorized();

        var discordEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:FplBot.EventHandlers.Discord"));
        var cmd = new ProcessGameweekStartedForGuildChannel("893932860162064414", "897565955587186838", 4);
        await discordEndpoint.Send(cmd);

        var slackEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:FplBot.EventHandlers.Slack"));
        var cmdSlack = new ProcessGameweekStartedForSlackWorkspace("t016b9n3u7p".ToUpper(), 4);
        await slackEndpoint.Send(cmdSlack);

        return TypedResults.Accepted("", cmd);
    }
}
