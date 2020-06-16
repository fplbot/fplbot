using Microsoft.AspNetCore.Builder;

namespace FplBot.WebApi.EventApi.Middlewares
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSlackbotEvents(this IApplicationBuilder app, string path)
        {
            app.MapWhen(ctx => SlackEventsChallengeMiddleware.ShouldRun(ctx, path), a => a.UseMiddleware<SlackEventsChallengeMiddleware>());
            app.MapWhen(ctx => SlackEventsUninstalledMiddleware.ShouldRun(ctx, path), a => a.UseMiddleware<SlackEventsUninstalledMiddleware>());
            app.MapWhen(ctx => SlackEventsMiddleware.ShouldRun(ctx, path), a => a.UseMiddleware<SlackEventsMiddleware>());
            return app;
        }
    }
}