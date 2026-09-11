using Discord.Net.Endpoints.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Hosting;

namespace FplBot.WebApi.Endpoints.Api.Oauth;

public static class InstallUrlEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/install-url", InstallUrl);
        group.MapGet("/install-url-discord", InstallUrlDiscord);
    }

    private static IResult InstallUrl(HttpContext httpContext, ILogger<Program> logger, IOptions<OAuthOptions> options)
    {
        logger.LogInformation("Installing");
        var original = new Uri(httpContext.Request.GetDisplayUrl());
        var redirectUri = new Uri(original, "/oauth/authorize");
        return TypedResults.Ok(new
        {
            redirectUri = $"https://slack.com/oauth/v2/authorize?&user_scope=&scope=app_mentions:read,chat:write,chat:write.customize,chat:write.public,users.profile:read,users:read,users:read.email,groups:read,channels:read&client_id={options.Value.CLIENT_ID}&redirect_uri={redirectUri}"
        });
    }

    private static IResult InstallUrlDiscord(HttpContext httpContext, ILogger<Program> logger, IOptions<DiscordOAuthOptions> discordOptions)
    {
        logger.LogInformation("Installing");
        var original = new Uri(httpContext.Request.GetDisplayUrl());
        var redirectUri = new Uri(original, "/oauth/discord/authorize");
        return TypedResults.Ok(new
        {
            redirectUri = $"https://discord.com/api/oauth2/authorize?client_id={discordOptions.Value.CLIENT_ID}&redirect_uri={redirectUri}&scope=bot%20applications.commands&permissions=309237844032&response_type=code"
        });
    }
}
