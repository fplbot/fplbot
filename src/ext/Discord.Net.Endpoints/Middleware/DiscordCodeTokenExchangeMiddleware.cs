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
        logger.LogInformation("Installing discord bot!");
        var redirect_uri = new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host.Value.ToString()}{ctx.Request.PathBase.Value}");
        var code = ctx.Request.Query["code"].FirstOrDefault();
        var guild_id = ctx.Request.Query["guild_id"].FirstOrDefault();

        if(code == null)
            await _next(ctx);

        var httpClient = new HttpClient();
        var parameters = new List<KeyValuePair<string,string>>
        {
            new ("code", code),
            new ("client_id", options.Value.CLIENT_ID),
            new ("client_secret", options.Value.CLIENT_SECRET),
            new ("grant_type", "authorization_code"),
            new ("redirect_uri", redirect_uri.ToString())
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
            await guildStore.Insert(new Guild(guild_id, guild_name));
            await options.Value.OnSuccess(guild_id, guild_name, provider);
            ctx.Response.Redirect(options.Value.SuccessRedirectUri);
        }
        else
        {
            logger.LogError($"Bad Oauth response! {response}");
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await ctx.Response.WriteAsync(jsonResponse);
        }
    }
}