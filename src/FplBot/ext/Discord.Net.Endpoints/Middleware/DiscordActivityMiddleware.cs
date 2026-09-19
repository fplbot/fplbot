namespace Discord.Net.Endpoints.Middleware;

internal class DiscordActivityMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        using var activity = DiscordDiagnostics.ActivitySource.StartActivity("DiscordWebhook");
        await next(context);
    }
}
