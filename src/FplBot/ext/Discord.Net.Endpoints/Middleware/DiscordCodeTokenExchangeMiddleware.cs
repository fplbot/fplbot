using System.Net;
using System.Text;
using System.Text.Json;
using Discord.Net.Endpoints.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Discord.Net.Endpoints.Middleware;

internal class DiscordCodeTokenExchangeMiddleware
{
    private readonly RequestDelegate _next;

    public DiscordCodeTokenExchangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext ctx, IOptions<DiscordOAuthOptions> options, IServiceProvider provider, IGuildStore guildStore, ILogger<DiscordCodeTokenExchangeMiddleware> logger)
    {
        var error = ctx.Request.Query["error"].FirstOrDefault();
        if (!string.IsNullOrEmpty(error))
        {
            var description = ctx.Request.Query["error_description"];
            logger.LogWarning($"Error received from Discord:{error}:{description}. Redirecting to error page.");
            var location = $"{options.Value.ErrorRedirectUri}?details={WebUtility.UrlEncode(error)}";
            ctx.Response.Redirect((location));
            return;
        }

        logger.LogInformation("Installing discord bot!");
        var redirectUri = new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host.Value}{ctx.Request.PathBase.Value}");
        var code = ctx.Request.Query["code"].FirstOrDefault();
        var guildId = ctx.Request.Query["guild_id"].FirstOrDefault();

        if(string.IsNullOrEmpty(code))
        {
            logger.LogWarning("No code received");
            var location = $"{options.Value.ErrorRedirectUri}?details=no_code";
            ctx.Response.Redirect(location);
            return;
        }

        var httpClient = new HttpClient();
        var parameters = new List<KeyValuePair<string,string>>
        {
            new ("code", code),
            new ("client_id", options.Value.CLIENT_ID),
            new ("client_secret", options.Value.CLIENT_SECRET),
            new ("grant_type", "authorization_code"),
            new ("redirect_uri", redirectUri.ToString())
        };

        var formUrlEncodedContent = new FormUrlEncodedContent(parameters);
        var requestContent = await formUrlEncodedContent.ReadAsStringAsync();
        var httpContent = new StringContent(requestContent, Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await httpClient.PostAsync("https://discord.com/api/oauth2/token", httpContent);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            var jsonDoc = JsonDocument.Parse(jsonResponse).RootElement;
            var guild = jsonDoc.GetProperty("guild");
            var guild_name = guild.GetProperty("name").GetString();
            logger.LogInformation($"Oauth response! ok:{jsonResponse}");
            await guildStore.Insert(new Guild(guildId, guild_name));
            await options.Value.OnSuccess(guildId, guild_name, provider);
            ctx.Response.Redirect(options.Value.SuccessRedirectUri);
        }
        else
        {
            logger.LogError($"Token exchange failed ({response.StatusCode})! Response: \n{jsonResponse}");
            var location = $"{options.Value.ErrorRedirectUri}?details=token_exchange_failed";
            ctx.Response.Redirect(location);
        }
    }
}
