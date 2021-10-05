using System;
using System.Threading.Tasks;
using Discord.Net.Endpoints.Middleware;
using Discord.Net.Endpoints.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Discord.Net.Endpoints.Hosting
{
    public static class IAppBuilderExtensions
    {
        public static IApplicationBuilder UseDiscordbot(this IApplicationBuilder app, bool enableAuth = true)
        {
            if (enableAuth)
                app.UseMiddleware<DiscordEventAuthMiddleware>();

            app.UseMiddleware<HttpItemsManager>();
            app.MapWhen(c => c.GetDiscordType() == 1, b => b.UseMiddleware<PingMiddleware>());
            app.MapWhen(c => c.GetDiscordType() == 2, b => b.UseMiddleware<SlashCommandsMiddleware>());
            app.MapWhen(c => c.IsUnhandledDiscordType(), b => b.UseMiddleware<UnknownTypeMiddleware>());
            return app;
        }

        /// <summary>
        /// NB! The path you run this middleware must:
        /// - match redirect_uri in your 1st redirect to Slack
        /// - be a valid redirect_uri in your Slack app configuration
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDiscordDistribution(this IApplicationBuilder app)
        {
            app.UseMiddleware<DiscordCodeTokenExchangeMiddleware>();
            return app;
        }
    }

    public class DiscordOAuthOptions
    {
        public DiscordOAuthOptions()
        {
            OnSuccess = (_,_,_) => Task.CompletedTask;
        }
        public string CLIENT_ID { get; set; }
        public string CLIENT_SECRET { get; set; }
        public string SuccessRedirectUri { get; set; } = "/success";
        public Func<string, string, IServiceProvider, Task> OnSuccess { get; set; }
    }
}

