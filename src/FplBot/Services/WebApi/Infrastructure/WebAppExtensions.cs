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

        // Always ProblemDetails, in every environment — there's no server-rendered HTML
        // surface left to protect with UseDeveloperExceptionPage (no Razor Pages, every
        // request is either the Vue SPA's static files, a signature-verified webhook, or
        // JSON consumed via fetch, which an HTML dev-exception-page can't usefully show
        // anyway). AddProblemDetails()'s CustomizeProblemDetails callback (see
        // WebApplicationBuilderExtensions) still attaches the exception detail in
        // Development, just as a JSON extension field instead of an HTML page.
        app.UseExceptionHandler();

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
        app.UseCors(CorsOriginValidator.CorsPolicyName);
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
        FplEndpoints.Map(api.MapGroup("/fpl").RequireCors(CorsOriginValidator.CorsPolicyName));
        SearchEndpoints.Map(api.MapGroup("/search").RequireCors(CorsOriginValidator.CorsPolicyName));
        InstallUrlEndpoints.Map(api.MapGroup("/oauth").RequireCors(CorsOriginValidator.CorsPolicyName));

        // Two separate /admin groups on purpose: login/logout/me carry mixed per-route auth
        // (see AdminAuthEndpoints), while everything else requires the IsAdmin policy as a
        // group-wide convention. No CORS policy here — admin is cookie-authenticated and
        // same-origin only by design.
        AdminAuthEndpoints.Map(api.MapGroup("/admin"));
        var admin = api.MapGroup("/admin").RequireAuthorization("IsAdmin");
        AdminSlackEndpoints.Map(admin);
        AdminSearchEndpoints.Map(admin);
        AdminDiscordEndpoints.Map(admin);
        AdminHealthEndpoints.Map(admin, env);

        // A plain MapFallbackToFile("index.html") would serve the SPA shell for *any*
        // unmatched request, including a typo'd /api/**, /debug/**, or webhook path —
        // which would then hand a 200 HTML response to what should be a 404 from the
        // backend (and, worse, dress it up as the Vue app's own funny 404 page, hiding a
        // real routing problem). Anything under a known backend prefix that didn't match
        // a real endpoint gets a genuine 404 (ProblemDetails, via UseStatusCodePages
        // above) instead; everything else falls through to the SPA, whose own Vue Router
        // catch-all renders the actual not-found page.
        var reservedBackendPrefixes = new[] { "/api", "/debug", "/oauth", "/events", "/discord" };
        app.MapFallback(async context =>
        {
            var path = context.Request.Path;
            if (reservedBackendPrefixes.Any(prefix => path.StartsWithSegments(prefix)))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html";
            var file = wwwrootProvider.GetFileInfo("index.html");
            await using var stream = file.CreateReadStream();
            await stream.CopyToAsync(context.Response.Body);
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
