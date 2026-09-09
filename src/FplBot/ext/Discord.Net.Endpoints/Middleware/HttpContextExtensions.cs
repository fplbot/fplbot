namespace Discord.Net.Endpoints.Middleware;

internal static class HttpContextExtensions
{
    public static int GetDiscordType(this HttpContext ctx)
    {
        object? ctxItem = ctx.Items[HttpItemKeys.TypeKey];
        if (ctxItem == null)
        {
            return -1;
        }
        return int.Parse(ctxItem!.ToString()!);
    }

    public static bool IsUnhandledDiscordType(this HttpContext ctx)
    {
        return ctx.Items.ContainsKey(HttpItemKeys.UnhandledKey);
    }
}
