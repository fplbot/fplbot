using Discord.Net.Endpoints.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Discord.Net.Endpoints.Middleware;

internal class DiscordEventAuthMiddleware
{
    private readonly RequestDelegate _next;

    public DiscordEventAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext ctx, ILogger<DiscordEventAuthMiddleware> logger)
    {
        bool success = false;
        try
        {
            var res = await ctx.AuthenticateAsync(DiscordEventsAuthenticationConstants.AuthenticationScheme);
            success = res.Succeeded;
        }
        catch (InvalidOperationException ioe)
        {
            throw new InvalidOperationException("Did you forget to call services.AddAuthentication().AddDiscordbotEvents()?", ioe);
        }

        if (success)
        {
            await _next(ctx);
        }
        else
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("UNAUTHORIZED");
        }
    }
}