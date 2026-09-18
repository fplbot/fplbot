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

    internal static IResult InstallUrl(HttpContext httpContext, ILogger<Program> logger, IOptions<OAuthOptions> options, string? returnTo = null)
    {
        logger.LogInformation("Installing");
        var original = new Uri(httpContext.Request.GetDisplayUrl());
        var redirectUri = new Uri(original, "/oauth/authorize");
        var state = ToSiteRelativeState(returnTo);
        return TypedResults.Ok(new
        {
            redirectUri =
                $"https://slack.com/oauth/v2/authorize?&user_scope=&scope=app_mentions:read,chat:write,chat:write.customize,chat:write.public,users.profile:read,users:read,users:read.email,groups:read,channels:read&client_id={options.Value.CLIENT_ID}&redirect_uri={redirectUri}{state}"
        });
    }

    internal static IResult InstallUrlDiscord(HttpContext httpContext, ILogger<Program> logger, IOptions<DiscordOAuthOptions> discordOptions,
        string? returnTo = null)
    {
        logger.LogInformation("Installing");
        var original = new Uri(httpContext.Request.GetDisplayUrl());
        var redirectUri = new Uri(original, "/oauth/discord/authorize");
        var state = ToSiteRelativeState(returnTo);
        return TypedResults.Ok(new
        {
            redirectUri =
                $"https://discord.com/api/oauth2/authorize?client_id={discordOptions.Value.CLIENT_ID}&redirect_uri={redirectUri}&scope=bot%20applications.commands&permissions=309237861440&response_type=code{state}"
        });
    }

    // Only a path on this site survives into `state`. It is validated again where it comes back,
    // since that is the half a third party can reach — this just avoids minting a state that
    // could never be honoured.
    private static string ToSiteRelativeState(string? returnTo) =>
        IsSiteRelativePath(returnTo) ? $"&state={WebUtility.UrlEncode(returnTo)}" : string.Empty;

    private static bool IsSiteRelativePath(string? path) =>
        !string.IsNullOrEmpty(path)
        && path[0] == '/'
        && (path.Length == 1 || (path[1] != '/' && path[1] != '\\'))
        && !path.Any(char.IsControl);
}
