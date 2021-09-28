using Microsoft.AspNetCore.Builder;
using Slackbot.Net.Endpoints.Middlewares;

namespace Slackbot.Net.Endpoints.Hosting
{
    public static class IAppBuilderExtensions
    {
        public static IApplicationBuilder UseSlackbot(this IApplicationBuilder app, bool enableAuth = true)
        {
            if (enableAuth)
                app.UseMiddleware<SlackbotEventAuthMiddleware>();

            app.UseMiddleware<HttpItemsManager>();
            app.MapWhen(Challenge.ShouldRun, b => b.UseMiddleware<Challenge>());
            app.MapWhen(Uninstall.ShouldRun, b => b.UseMiddleware<Uninstall>());
            app.MapWhen(AppMentionEvents.ShouldRun, b => b.UseMiddleware<AppMentionEvents>());
            app.MapWhen(MemberJoinedEvents.ShouldRun, b => b.UseMiddleware<MemberJoinedEvents>());
            app.MapWhen(AppHomeOpenedEvents.ShouldRun, b => b.UseMiddleware<AppHomeOpenedEvents>());
            app.MapWhen(InteractiveEvents.ShouldRun, b => b.UseMiddleware<InteractiveEvents>());

            return app;
        }

        /// <summary>
        /// NB! The path you run this middleware must:
        /// - match redirect_uri in your 1st redirect to Slack
        /// - be a valid redirect_uri in your Slack app configuration
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseSlackbotDistribution(this IApplicationBuilder app)
        {
            app.UseMiddleware<SlackbotCodeTokenExchangeMiddleware>();
            return app;
        }
    }
}

