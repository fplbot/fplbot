using System.Net;
using System.Text;
using System.Text.Json;
using Discord.Net.Endpoints.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Discord.Net.Endpoints.Middleware;

internal class DiscordCodeTokenExchangeMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task Invoke(HttpContext ctx, IOptions<DiscordOAuthOptions> options, IServiceProvider provider, IGuildInstallationHandler guildInstallationHandler, IHttpClientFactory httpClientFactory, ILogger<DiscordCodeTokenExchangeMiddleware> logger)
    {
        var error = ctx.Request.Query["error"].FirstOrDefault();
        if (!string.IsNullOrEmpty(error))
        {
            var description = ctx.Request.Query["error_description"];
            logger.LogWarning($"Error received from Discord:{error}:{description}. Redirecting to error page.");
            ctx.Response.Redirect(ErrorRedirect(options.Value.ErrorRedirectUri, error, ctx.Request.Query["state"].FirstOrDefault()));
            return;
        }

        logger.LogInformation("Installing discord bot!");
        var redirectUri = new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host.Value}{ctx.Request.PathBase.Value}");
        var code = ctx.Request.Query["code"].FirstOrDefault();
        var guildId = ctx.Request.Query["guild_id"].FirstOrDefault();

        if(string.IsNullOrEmpty(code))
        {
            logger.LogWarning("No code received");
            ctx.Response.Redirect(ErrorRedirect(options.Value.ErrorRedirectUri, "no_code", ctx.Request.Query["state"].FirstOrDefault()));
            return;
        }

        var httpClient = httpClientFactory.CreateClient(Hosting.ServiceCollectionExtensions.TokenExchangeHttpClient);
        var parameters = new List<KeyValuePair<string,string>>
        {
            new ("code", code),
            new ("client_id", options.Value.CLIENT_ID ?? string.Empty),
            new ("client_secret", options.Value.CLIENT_SECRET ?? string.Empty),
            new ("grant_type", "authorization_code"),
            new ("redirect_uri", redirectUri.ToString())
        };

        var formUrlEncodedContent = new FormUrlEncodedContent(parameters);
        var requestContent = await formUrlEncodedContent.ReadAsStringAsync();
        var httpContent = new StringContent(requestContent, Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await httpClient.PostAsync("oauth2/token", httpContent);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var stateTheAppSent = ctx.Request.Query["state"].FirstOrDefault();
        if (response.IsSuccessStatusCode)
        {
            var jsonDoc = JsonDocument.Parse(jsonResponse).RootElement;
            var guild = jsonDoc.GetProperty("guild");
            var guild_name = guild.GetProperty("name").GetString();
            logger.LogInformation($"Oauth response! ok:{jsonResponse}");
            await guildInstallationHandler.Install(new Guild(guildId ?? string.Empty, guild_name ?? string.Empty));
            ctx.Response.Redirect(SuccessRedirect(options.Value.SuccessRedirectUri, stateTheAppSent));
        }
        else
        {
            logger.LogError($"Token exchange failed ({response.StatusCode})! Response: \n{jsonResponse}");
            ctx.Response.Redirect(ErrorRedirect(options.Value.ErrorRedirectUri, "token_exchange_failed", stateTheAppSent));
        }
    }

    // An install that does not complete carries `state` back too, so the app can put the user
    // where they started rather than stranding them on a generic page.
    internal static string ErrorRedirect(string errorRedirectUri, string? details, string? state)
    {
        var query = new Dictionary<string, string?> { ["details"] = details };
        if (!string.IsNullOrEmpty(state))
        {
            query["state"] = state;
        }

        return QueryHelpers.AddQueryString(errorRedirectUri, query);
    }

    // `state` is opaque to this library — only the app that sent it knows what it means, so it
    // rides back to the app's own success page untouched rather than being interpreted here.
    internal static string SuccessRedirect(string successRedirectUri, string? state) =>
        string.IsNullOrEmpty(state)
            ? successRedirectUri
            : QueryHelpers.AddQueryString(successRedirectUri, "state", state);
}
