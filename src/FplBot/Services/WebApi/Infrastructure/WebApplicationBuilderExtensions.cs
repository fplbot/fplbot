using System.Text.Json.Serialization;
using AspNet.Security.OAuth.Slack;
using CronBackgroundServices;
using Discord.Net.Endpoints.Authentication;
using Discord.Net.Endpoints.Hosting;
using Fpl.Search;
using FplBot.Config;
using FplBot.Discord;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.WebApi.Configurations;
using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
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

        services.AddOptions<DiscordWebOptions>()
            .Bind(configuration)
            .ValidateWithFluentValidation(new DiscordWebOptionsValidator())
            .ValidateOnStart();

        services.AddRecurrer<GuildStatusChecker>();

        services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redisConn)
            .SetApplicationName("fplbot");

        // In dev, /success is served by the Vite dev server (not this backend), so send the
        // browser there after the OAuth callback. /error stays on the backend — it's still a
        // Razor Page shared with the admin login flow, not part of the Vue SPA.
        var successUri = env.IsDevelopment() ? "http://localhost:5173/success" : "/success";
        var errorUri = "/error";

        services.AddSlackbotDistribution(c =>
        {
            c.CLIENT_ID = configuration["CLIENT_ID"];
            c.CLIENT_SECRET = configuration["CLIENT_SECRET"];
            c.SuccessRedirectUri = $"{successUri}?type=slack";
            c.OnSuccess = async (teamId, teamName, s) =>
            {
                var msg = s.GetRequiredService<IPublishEndpoint>();
                await msg.Publish(new AppInstalled(teamId, teamName, ChatPlatform.Slack));
            };
        });

        services.AddDiscordBotDistribution(c =>
        {
            c.CLIENT_ID = configuration["DISCORD_CLIENT_ID"];
            c.CLIENT_SECRET = configuration["DISCORD_CLIENT_SECRET"];
            c.SuccessRedirectUri = $"{successUri}?type=discord";
            c.ErrorRedirectUri = errorUri;
            c.OnSuccess = async (guildId, guildName, s) =>
            {
                var msg = s.GetRequiredService<IPublishEndpoint>();
                await msg.Publish(new AppInstalled(guildId, guildName, ChatPlatform.Discord));
            };
        });

        services.Configure<AnalyticsOptions>(configuration);
        services.AddFplBotSlackWebEndpoints(configuration, redisConn, env);
        services.AddFplBotDiscordWebEndpoints(configuration, redisConn, env);
        services.AddIndexingServices(configuration, redisConn);

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
                c.Scope.Add("identity.team");
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

        services.AddAuthorization();
        services.AddOptions<AuthorizationOptions>()
            .Configure<IOptions<SlackAdminOptions>>((authOptions, adminOpts) =>
            {
                var admin = adminOpts.Value;
                var allowedUserIds = (admin.AllowedUserIds ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                authOptions.AddPolicy("IsAdmin", b =>
                {
                    b.RequireAuthenticatedUser();
                    if (!string.IsNullOrEmpty(admin.AllowedTeamId))
                        b.RequireClaim("urn:slack:team_id", admin.AllowedTeamId);
                    if (allowedUserIds.Length > 0)
                        b.RequireClaim("urn:slack:user_id", allowedUserIds);
                });
            });

        var mvcBuilder = services
            .AddRazorPages()
            .AddRazorPagesOptions(options =>
            {
                options.RootDirectory = "/Services/WebApi/Pages";
            });

        if (env.IsDevelopment())
            mvcBuilder.AddRazorRuntimeCompilation();

        services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddMemoryCache();

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
            options.AddPolicy(CorsOriginValidator.CustomCorsPolicyName, p =>
                p.SetIsOriginAllowed(CorsOriginValidator.ValidateOrigin).AllowAnyHeader().AllowAnyMethod());
        });

        services.AddHttpContextAccessor();
        services.Configure<BlockedIpOptions>(configuration.GetSection("IpBlocking"));
    }
}
