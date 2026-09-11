using Discord.Net.Endpoints.Hosting;
using FplBot.WebApi.Endpoints.Api.Admin;
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
        else
        {
            // Turns any unhandled exception into a clean application/problem+json response
            // (via the IProblemDetailsService registered by AddProblemDetails()) instead of
            // an empty 500 body or a leaked stack trace.
            app.UseExceptionHandler();
        }

        // Fills in an application/problem+json body for any response that already has an
        // error status code but hasn't written content yet — e.g. TypedResults.NotFound(),
        // the cookie auth handler's 401/403 (see WebApplicationBuilderExtensions), or the
        // 400 from a failed minimal-API parameter binding. Every /api/** error response
        // should be a problem body, not a bare status code with an empty body.
        app.UseStatusCodePages();

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

        // Two separate /admin groups on purpose: login/logout/me carry mixed per-route auth
        // (see AdminAuthEndpoints), while everything else requires the IsAdmin policy as a
        // group-wide convention. No CORS policy here — admin is cookie-authenticated and
        // same-origin only by design.
        AdminAuthEndpoints.Map(api.MapGroup("/admin"));
        var admin = api.MapGroup("/admin").RequireAuthorization("IsAdmin");
        AdminTeamsEndpoints.Map(admin);
        AdminBroadcastEndpoints.Map(admin);
        AdminIndexingEndpoints.Map(admin);
        AdminDiscordEndpoints.Map(admin);

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
