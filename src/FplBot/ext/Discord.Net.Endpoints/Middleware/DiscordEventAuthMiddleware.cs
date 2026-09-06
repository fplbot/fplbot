using Discord.Net.Endpoints.Authentication;
using Microsoft.AspNetCore.Authentication;

namespace Discord.Net.Endpoints.Middleware;

internal class DiscordEventAuthMiddleware(RequestDelegate next)
{
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
            await next(ctx);
        }
        else
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("UNAUTHORIZED");
        }
    }
}
