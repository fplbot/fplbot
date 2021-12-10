using System.Text.Json.Serialization;
using AspNet.Security.OAuth.Slack;
using Discord.Net.Endpoints.Authentication;
using Discord.Net.Endpoints.Hosting;
using Fpl.Search;
using Fpl.Search.Data.Abstractions;
using Fpl.Search.Data.Repositories;
using FplBot.Discord;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Data;
using FplBot.WebApi.Configurations;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using NServiceBus;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Slackbot.Net.Endpoints.Authentication;
using Slackbot.Net.Endpoints.Hosting;
using StackExchange.Redis;

namespace FplBot.WebApi.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    public static void ConfigureWebApp(this WebApplicationBuilder builder)
    {
        builder.Host.UseMessaging();
        builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
            loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.WithCorrelationId()
                .Enrich.WithCorrelationIdHeader()
                .WriteTo.Console(
                    outputTemplate:
                    "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                    theme: ConsoleTheme.None));

        var services = builder.Services;
        var configuration = builder.Configuration;
        var env = builder.Environment;

        var opts = new RedisOptions { REDIS_URL = Environment.GetEnvironmentVariable("REDIS_URL") };
        var options = new ConfigurationOptions
        {
            ClientName = opts.GetRedisUsername,
            Password = opts.GetRedisPassword,
            EndPoints = {opts.GetRedisServerHostAndPort}
        };
        var conn =  ConnectionMultiplexer.Connect(options);

        services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(conn)
            .SetApplicationName(
                "fplbot"); // set static so cookies are not encrypted differently after a reboot/deploy. https://github.com/dotnet/aspnetcore/issues/2513#issuecomment-354683162
        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        var successUri = env.IsProduction() ? "https://www.fplbot.app/success" : "https://test.fplbot.app/success";
        services.AddSlackbotDistribution(c =>
        {
            c.CLIENT_ID = configuration["CLIENT_ID"];
            c.CLIENT_SECRET = configuration["CLIENT_SECRET"];
            c.SuccessRedirectUri = $"{successUri}?type=slack";
            c.OnSuccess = async (teamId,teamName, s) =>
            {
                var msg = s.GetService<IMessageSession>();
                await msg.Publish(new AppInstalled(teamId, teamName));
            };
        });
        services.AddDiscordBotDistribution(c =>
        {
            c.CLIENT_ID = configuration["DISCORD_CLIENT_ID"];
            c.CLIENT_SECRET = configuration["DISCORD_CLIENT_SECRET"];
            c.SuccessRedirectUri = $"{successUri}?type=discord";
            c.OnSuccess = async (guildId,guildName, s) =>
            {
                var msg = s.GetService<IMessageSession>();
                await msg.Publish(new AppInstalled(guildId, guildName));
            };
        });
        services.Configure<AnalyticsOptions>(configuration);
        services.AddReducedHttpClientFactoryLogging();
        services.AddFplBot(configuration, conn);
        services.AddStackExchangeRedisCache(o => o.ConfigurationOptions = options);
        services.AddFplBotDiscord(configuration, conn);
        services.AddVerifiedEntries(configuration);


        services.AddMediatR(typeof(WebApplicationBuilderExtensions));

        // Used in admin pages:
        services.AddSingleton<ILeagueIndexBookmarkProvider, LeagueIndexRedisBookmarkProvider>();
        services.AddSingleton<IEntryIndexBookmarkProvider, EntryIndexRedisBookmarkProvider>();


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
                c.ClientId = configuration.GetValue<string>("CLIENT_ID");
                c.ClientSecret = configuration.GetValue<string>("CLIENT_SECRET");
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
                c.SigningSecret = configuration.GetValue<string>("CLIENT_SIGNING_SECRET");
            })
            .AddDiscordbotEvents(c =>
            {
                c.PublicKey = configuration.GetValue<string>("DISCORD_PUBLICKEY");
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
        {
            mvcBuilder.AddRazorRuntimeCompilation();
        }

        services.Configure<RouteOptions>(o =>
        {
            o.LowercaseQueryStrings = true;
            o.LowercaseUrls = true;
        });
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        services.AddCors(options =>
        {
            options.AddPolicy(CorsOriginValidator.CustomCorsPolicyName, p =>
                p.SetIsOriginAllowed(CorsOriginValidator.ValidateOrigin).AllowAnyHeader().AllowAnyMethod()
            );
        });
        services.AddHttpContextAccessor();

    }
}
