using System.Text.Json.Serialization;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Slack;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CronBackgroundServices;
using Discord.Net.Endpoints.Authentication;
using Discord.Net.Endpoints.Hosting;
using Fpl.Search;
using FplBot.Config;
using FplBot.Data.Web;
using FplBot.Discord;
using FplBot.Integrations.WebPush;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.WebApi.Admin;
using FplBot.WebApi.Configurations;
using FplBot.WebApi.Mcp;
using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using Slackbot.Net.Endpoints.Authentication;
using Slackbot.Net.Endpoints.Hosting;
using StackExchange.Redis;

namespace FplBot.WebApi.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    public static void ConfigureWebApp(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env, ConnectionMultiplexer redisConn)
    {
        services.AddOptions<SlackOptions>()
            .Bind(configuration)
            .ValidateWithFluentValidation(new SlackOptionsValidator())
            .ValidateOnStart();

        services.AddOptions<SlackAdminOptions>()
            .Bind(configuration.GetSection("admin"))
            .ValidateWithFluentValidation(new SlackAdminOptionsValidator())
            .ValidateOnStart();

        services.Configure<AdminAllowedEmails>(configuration.GetSection("admin"));

        services.AddOptions<DiscordWebOptions>()
            .Bind(configuration)
            .ValidateWithFluentValidation(new DiscordWebOptionsValidator())
            .ValidateOnStart();

        services.AddRecurrer<GuildMemberCountChecker>();
        services.AddRecurrer<SlackChannelMemberCountChecker>();

        services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redisConn)
            .SetApplicationName("fplbot");

        // In dev, /success is served by the Vite dev server (not this backend), so send the
        // browser there after the OAuth callback. /error stays on the backend — it's still a
        // Razor Page shared with the admin login flow, not part of the Vue SPA.
        var successUri = env.IsLocal() ? "http://localhost:5173/success" : "/success";
        var installFailedUri = env.IsLocal() ? "http://localhost:5173/install-cancelled" : "/install-cancelled";

        services.AddSlackbotDistribution(c =>
        {
            c.CLIENT_ID = configuration["CLIENT_ID"];
            c.CLIENT_SECRET = configuration["CLIENT_SECRET"];
            c.SuccessRedirectUri = $"{successUri}?type=slack";
        });
        services.AddOptions<OAuthOptions>()
            .ValidateWithFluentValidation(new OAuthOptionsValidator())
            .ValidateOnStart();

        services.AddDiscordBotDistribution(c =>
        {
            c.CLIENT_ID = configuration["DISCORD_CLIENT_ID"];
            c.CLIENT_SECRET = configuration["DISCORD_CLIENT_SECRET"];
            c.SuccessRedirectUri = $"{successUri}?type=discord";
            c.ErrorRedirectUri = installFailedUri;
        });

        services.Configure<AnalyticsOptions>(configuration);
        services.AddFplBotSlackWebEndpoints(configuration, redisConn, env);
        services.AddFplBotDiscordWebEndpoints(configuration, redisConn, env);
        services.AddIndexingServices(configuration, redisConn);
        services.AddWebPushSubscribers();
        services.Configure<WebPushOptions>(configuration.GetSection("WebPush"));

        // Only otherwise registered for the EventPublishers service (Fpl.EventPublishers.States.LineupState).
        // The admin "publish now" lineups action runs here in WebApi, which is a separate container in
        // production, so it needs its own registration rather than relying on EventPublishers' being active.
        // AddPulseLiveClient no-ops if EventPublishers already registered it in this same process.
        services.AddPulseLiveClient();

        var asbConnectionString = configuration["ASB_CONNECTIONSTRING"]
                                  ?? throw new InvalidOperationException("Service bus connection string not configured. Set ASB_CONNECTIONSTRING.");
        services.AddSingleton(new ServiceBusAdministrationClient(asbConnectionString));
        services.AddSingleton(new ServiceBusClient(asbConnectionString));
        services.AddSingleton<AdminErrorQueueService>();
        services.AddSingleton<AdminErrorQueueJobRunner>();

        services.AddAuthentication(options =>
            {
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(o =>
            {
                o.Cookie.Name = "fplbot-admin";
                o.Cookie.SameSite = SameSiteMode.Lax;
                // Every resource behind this cookie now lives under /api/admin/** and is
                // called via fetch from the Vue admin SPA, not via server-rendered pages —
                // so on auth failure, return plain status codes instead of the default
                // redirect-to-login/redirect-to-access-denied behavior (which would make a
                // fetch() call transparently follow a redirect into an HTML page). The
                // deliberate "log in" action (AdminAuthEndpoints.Login) challenges the Slack
                // scheme directly by name, so it never goes through OnRedirectToLogin.
                o.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                o.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddSlack(c =>
            {
                // identity.team stays for display purposes only (teamName/teamId in /me) — it's
                // identity.email, not team membership, that now decides who's let in.
                c.Scope.Add("identity.team");
                c.Scope.Add("identity.email");
                c.Events.OnRemoteFailure = r =>
                {
                    var errorMsg = r.Request.Query["error"];
                    r.Response.Redirect($"/error?msg={errorMsg}");
                    r.HandleResponse();
                    return Task.FromResult(0);
                };
            })
            .AddDiscord(c =>
            {
                c.Scope.Add("email");
                c.Events.OnRemoteFailure = r =>
                {
                    var errorMsg = r.Request.Query["error"];
                    r.Response.Redirect($"/error?msg={errorMsg}");
                    r.HandleResponse();
                    return Task.FromResult(0);
                };
            })
            .AddSlackbotEvents(c =>
            {
                c.SigningSecret = configuration.GetValue<string>("CLIENT_SIGNING_SECRET") ?? "";
            })
            .AddDiscordbotEvents(c =>
            {
                c.PublicKey = configuration.GetValue<string>("DISCORD_PUBLICKEY") ?? "";
            });

        services.AddSingleton<IPostConfigureOptions<SlackAuthenticationOptions>>(sp =>
            new PostConfigureOptions<SlackAuthenticationOptions>(
                SlackAuthenticationDefaults.AuthenticationScheme,
                opts =>
                {
                    var admin = sp.GetRequiredService<IOptions<SlackAdminOptions>>().Value;
                    opts.ClientId = admin.SlackClientId ?? "";
                    opts.ClientSecret = admin.SlackClientSecret ?? "";
                }));

        // Discord admin login reuses the same Discord application as the bot (root
        // DISCORD_CLIENT_ID/DISCORD_CLIENT_SECRET, already bound as DiscordWebOptions) rather than
        // a dedicated admin app like Slack has — there's no separate "admin" Discord application.
        services.AddSingleton<IPostConfigureOptions<DiscordAuthenticationOptions>>(sp =>
            new PostConfigureOptions<DiscordAuthenticationOptions>(
                DiscordAuthenticationDefaults.AuthenticationScheme,
                opts =>
                {
                    var discord = sp.GetRequiredService<IOptions<DiscordWebOptions>>().Value;
                    opts.ClientId = discord.DISCORD_CLIENT_ID ?? "";
                    opts.ClientSecret = discord.DISCORD_CLIENT_SECRET ?? "";
                }));

        services.AddSingleton<IAuthorizationHandler, IsAdminAuthorizationHandler>();
        services.AddAuthorization(o =>
        {
            o.AddPolicy("IsAdmin", b =>
            {
                b.RequireAuthenticatedUser();
                b.AddRequirements(new IsAdminRequirement());
            });
        });

        services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Backs UseExceptionHandler()/UseStatusCodePages() in WebAppExtensions — every
        // error response from /api/** should be application/problem+json, never a bare
        // status code or an unhandled-exception 500 with no body. In Development, also
        // attach the actual exception (message + stack trace) as an extension field —
        // this is the dev-diagnostics job UseDeveloperExceptionPage used to do, just
        // delivered as JSON since there's no HTML page rendering it that a browser
        // navigation would show; a fetch-based frontend can't use an HTML error page
        // anyway. Never done outside Development — would leak internals.
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (!context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsLocal())
                    return;

                var exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception != null)
                {
                    context.ProblemDetails.Extensions["exception"] = exception.ToString();
                }
            };
        });

        services.Configure<RouteOptions>(o =>
        {
            o.LowercaseQueryStrings = true;
            o.LowercaseUrls = true;
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOriginValidator.CorsPolicyName, p =>
                p.SetIsOriginAllowed(CorsOriginValidator.ValidateOrigin).AllowAnyHeader().AllowAnyMethod());
        });

        services.AddHttpContextAccessor();
        services.Configure<BlockedIpOptions>(configuration.GetSection("IpBlocking"));

        services.AddMcpServer().WithHttpTransport().WithTools<FplMcpTools>().WithResources<FplDomainResource>().WithUsageLogging();
    }
}
