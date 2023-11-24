using Discord.Net.Endpoints.Hosting;
using FplBot.WebApi.Endpoints.Test;
using Serilog;
using Slackbot.Net.Endpoints.Hosting;

namespace FplBot.WebApi.Infrastructure;

public static class WebAppExtensions
{
    public static void UseWebApp(this WebApplication app)
    {
        var env = app.Environment;
        app.UseSerilogRequestLogging();
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseForwardedHeaders();
        if(!env.IsDevelopment())
            app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors(CorsOriginValidator.CustomCorsPolicyName);
        app.UseCookiePolicy();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Map("/oauth/authorize", a => a.UseSlackbotDistribution());
        app.Map("/events", a => a.UseSlackbot(enableAuth: !env.IsDevelopment()));
        app.Map("/oauth/discord/authorize", a => a.UseDiscordDistribution());
        app.Map("/discord/events", a => a.UseDiscordbot(enableAuth: !env.IsDevelopment()));
        app.UseMinimalEndpoints(
            ("/debug", TestEndpoints.Map)
        );
        app.MapControllers().RequireCors(CorsOriginValidator.CustomCorsPolicyName);
        app.MapRazorPages();
    }

    private static void UseMinimalEndpoints(this WebApplication app, params (string BaseRoute, Action<WebApplication, string> RouteToEndpoint)[] mappings)
    {
        foreach (var mapping in mappings)
        {
            mapping.RouteToEndpoint(app, mapping.BaseRoute);
        }
    }
}
