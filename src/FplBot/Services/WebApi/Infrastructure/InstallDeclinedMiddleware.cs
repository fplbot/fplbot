using Microsoft.AspNetCore.WebUtilities;

namespace FplBot.WebApi.Infrastructure;

// Slack and Discord send the user back to the callback with `error` instead of `code` when they
// press Cancel on the consent screen. That is a decision, not a failure, so it never reaches the
// install middleware — which would otherwise try to exchange a code it never got.
public class InstallDeclinedMiddleware(RequestDelegate next, string cancelledPage, ILogger<InstallDeclinedMiddleware> logger)
{
    public async Task Invoke(HttpContext ctx)
    {
        var error = ctx.Request.Query["error"].FirstOrDefault();
        if (string.IsNullOrEmpty(error))
        {
            await next(ctx);
            return;
        }

        logger.LogInformation("Install was not completed: {Error}", error);

        var query = new Dictionary<string, string?> { ["reason"] = error };
        if (ctx.Request.Query["state"].FirstOrDefault() is { Length: > 0 } state)
        {
            query["state"] = state;
        }

        ctx.Response.Redirect(QueryHelpers.AddQueryString(cancelledPage, query));
    }
}
