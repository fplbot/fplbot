using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.WebApi.Endpoints.Test.GameweekEnd;

public static class TestGwEnd
{
    public static async Task<IResult> Get(IWebHostEnvironment env, ISendEndpointProvider sendEndpointProvider)
    {
        if (env.IsProduction())
            return TypedResults.Unauthorized();

        var discordEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:FplBot.EventHandlers.Discord"));
        var cmd = new PublishGameweekFinishedToGuild("893932860162064414", "897565955587186838", 1996879, 4);
        await discordEndpoint.Send(cmd);

        var slackEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:FplBot.EventHandlers.Slack"));
        var cmdSlack = new PublishStandingsToSlackWorkspace("t016b9n3u7p".ToUpper(), "#fplbot-test", 1996879, 4);
        await slackEndpoint.Send(cmdSlack);

        return TypedResults.Accepted("", cmd);
    }
}
