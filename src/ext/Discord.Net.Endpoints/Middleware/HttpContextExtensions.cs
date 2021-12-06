using Microsoft.AspNetCore.Http;

namespace Discord.Net.Endpoints.Middleware;

internal static class HttpContextExtensions
{
    public static int GetDiscordType(this HttpContext ctx)
    {
        return int.Parse(ctx.Items[HttpItemKeys.TypeKey].ToString());
    }

    public static bool IsUnhandledDiscordType(this HttpContext ctx)
    {
        return ctx.Items.ContainsKey(HttpItemKeys.UnhandledKey);
    }
}
