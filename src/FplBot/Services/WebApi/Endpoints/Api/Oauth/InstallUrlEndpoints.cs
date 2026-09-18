using System.Net;
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

    internal static IResult InstallUrlDiscord(HttpContext httpContext, ILogger<Program> logger, IOptions<DiscordOAuthOptions> discordOptions, string? returnTo = null)
    {
        logger.LogInformation("Installing");
        var original = new Uri(httpContext.Request.GetDisplayUrl());
        var redirectUri = new Uri(original, "/oauth/discord/authorize");
        var state = ToSiteRelativeState(returnTo);
        return TypedResults.Ok(new
        {
            redirectUri = $"https://discord.com/api/oauth2/authorize?client_id={discordOptions.Value.CLIENT_ID}&redirect_uri={redirectUri}&scope=bot%20applications.commands&permissions=309237861440&response_type=code{state}"
        });
    }

    // Only a path on this site survives into `state` — an absolute or protocol-relative url
    // would turn the OAuth callback into an open redirect.
    private static string ToSiteRelativeState(string? returnTo) =>
        string.IsNullOrEmpty(returnTo) || returnTo[0] != '/' || returnTo.StartsWith("//")
            ? string.Empty
            : $"&state={WebUtility.UrlEncode(returnTo)}";
}
