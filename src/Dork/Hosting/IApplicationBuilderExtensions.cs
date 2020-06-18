using Dork.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Dork.Hosting
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSlackbotEvents(this IApplicationBuilder app, string path)
        {
            app.MapWhen(c => IsSlackRequest(c, path), a =>
            {
                a.UseMiddleware<HttpItemsManager>();
                a.MapWhen(Challenge.ShouldRun, b => b.UseMiddleware<Challenge>());
                a.MapWhen(Uninstall.ShouldRun, b => b.UseMiddleware<Uninstall>());
                a.MapWhen(Events.ShouldRun, b => b.UseMiddleware<Events>());            
            });
   
            return app;
        }

        private static bool IsSlackRequest(HttpContext ctx, string path)
        {
            return ctx.Request.Path == path && ctx.Request.Method == "POST" && ctx.Request.ContentType == "application/json";
        }
    }
}