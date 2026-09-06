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
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;
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

        services.AddOptions<DiscordWebOptions>()
            .Bind(configuration)
            .ValidateWithFluentValidation(new DiscordWebOptionsValidator())
            .ValidateOnStart();

        services.AddRecurrer<GuildStatusChecker>();

        services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redisConn)
            .SetApplicationName("fplbot");

        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        var successUri = env.IsProduction() ? "https://www.fplbot.app/success" : "https://test.fplbot.app/success";
        var errorUri = env.IsProduction() ? "https://www.fplbot.app/error" : "https://test.fplbot.app/error";

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
        services.AddFplBotSlackWebEndpoints(configuration, redisConn);
        services.AddFplBotDiscordWebEndpoints(configuration, redisConn);
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
                o.AccessDeniedPath = "/forbidden";
                o.ReturnUrlParameter = "r";
                o.ForwardChallenge = SlackAuthenticationDefaults.AuthenticationScheme;
            })
            .AddSlack(c =>
            {
                c.ClientId = configuration.GetValue<string>("CLIENT_ID") ?? "";
                c.ClientSecret = configuration.GetValue<string>("CLIENT_SECRET") ?? "";
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

        services.AddAuthorization(options =>
        {
            options.AddPolicy("IsAdmin", b =>
            {
                b.RequireClaim("urn:slack:team_id", "T016B9N3U7P");
                b.RequireClaim("urn:slack:user_id", "U016CP6EPR8", "U0172HKTB08", "U016CSWNXAP");
            });
        });

        var mvcBuilder = services
            .AddRazorPages()
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AuthorizeFolder("/admin", "IsAdmin");
                options.Conventions.AllowAnonymousToPage("/*");
            });

        if (env.IsDevelopment())
            mvcBuilder.AddRazorRuntimeCompilation();

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
