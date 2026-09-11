using Discord.Net.Endpoints.Hosting;
using FplBot.WebApi.Endpoints.Api.Fpl;
using FplBot.WebApi.Endpoints.Api.Oauth;
using FplBot.WebApi.Endpoints.Api.Search;
using FplBot.WebApi.Endpoints.Test;
using Microsoft.Extensions.FileProviders;
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
        app.UseMiddleware<BlockedIpMiddleware>();
        if(!env.IsDevelopment())
            app.UseHttpsRedirection();

        var wwwrootProvider = new PhysicalFileProvider(
            Path.Combine(app.Environment.ContentRootPath, "Services", "WebApi", "wwwroot"));
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = wwwrootProvider
        });
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

        var api = app.MapGroup("/api");
        FplEndpoints.Map(api.MapGroup("/fpl").RequireCors(CorsOriginValidator.CustomCorsPolicyName));
        SearchEndpoints.Map(api.MapGroup("/search").RequireCors(CorsOriginValidator.CustomCorsPolicyName));
        InstallUrlEndpoints.Map(api.MapGroup("/oauth").RequireCors(CorsOriginValidator.CustomCorsPolicyName));

        app.MapControllers().RequireCors(CorsOriginValidator.CustomCorsPolicyName);
        app.MapRazorPages();
        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            FileProvider = wwwrootProvider
        });
    }

    private static void UseMinimalEndpoints(this WebApplication app, params (string BaseRoute, Action<WebApplication, string> RouteToEndpoint)[] mappings)
    {
        foreach (var mapping in mappings)
        {
            mapping.RouteToEndpoint(app, mapping.BaseRoute);
        }
    }
}
