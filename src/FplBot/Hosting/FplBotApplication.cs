using System.Net.Security;
using FplBot.Services.EventHandlers;
using FplBot.Services.EventPublishers;
using FplBot.Services.SearchIndexer;
using FplBot.Services.WebApi;
using MassTransit;
using MassTransit.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;

namespace FplBot.Hosting;

public static class FplBotApplication
{
    private static readonly IFplBotService[] AllServices =
    [
        new WebApiService(),
        new EventHandlersService(),
        new EventPublishersService(),
        new SearchIndexerService()
    ];

    public static async Task RunAsync(string[] args, IReadOnlyList<FplBotService> activeServices)
    {
        var active = AllServices.Where(s => activeServices.Contains(s.ServiceType)).ToList();
        if (active.Any(s => s.ServiceType == FplBotService.WebApi))
            await RunAsWebApplication(args, active);
        else
            await RunAsWorkerHost(args, active);
    }

    private static async Task RunAsWebApplication(string[] args, List<IFplBotService> active)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog((ctx, lc) => ConfigureSerilog(ctx, lc, active));
        var port = Environment.GetEnvironmentVariable("PORT") ?? "1337";
        // Slack requires OAuth redirect_uris to be https — even for localhost. In dev, serve
        // https on localhost using the trusted ASP.NET Core dev cert (`dotnet dev-certs https
        // --trust`) — the cert is issued for CN=localhost, so bind that host specifically
        // rather than "+". In prod (Heroku/containers), TLS is terminated at the platform
        // router and the app must stay reachable on all interfaces, so keep "+" and http.
        builder.WebHost.UseUrls(builder.Environment.IsDevelopment()
            ? $"https://localhost:{port}"
            : $"http://+:{port}");

        var redisConn = BuildRedisConnection(builder.Configuration);
        ConfigureServices(builder.Services, builder.Configuration, redisConn, builder.Environment, active,
            cfg => ConfigureAzureServiceBus(cfg, builder.Configuration));

        var app = builder.Build();
        foreach (var svc in active)
            svc.ConfigureApp(app);

        await app.RunAsync();
    }

    private static async Task RunAsWorkerHost(string[] args, List<IFplBotService> active)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog((ctx, lc) => ConfigureSerilog(ctx, lc, active))
            .ConfigureServices((ctx, services) =>
            {
                var redisConn = BuildRedisConnection(ctx.Configuration);
                ConfigureServices(services, ctx.Configuration, redisConn, ctx.HostingEnvironment, active,
                    cfg => ConfigureAzureServiceBus(cfg, ctx.Configuration));
            })
            .Build();

        await host.RunAsync();
    }

    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration config,
        ConnectionMultiplexer redisConn,
        IHostEnvironment env,
        List<IFplBotService> active,
        Action<IBusRegistrationConfigurator> configureBus)
    {
        ConfigureCommon(services, config, redisConn);
        services.AddMassTransit(x =>
        {
            foreach (var svc in active)
                svc.ConfigureMassTransit(x);
            x.AddConfigureEndpointsCallback((_, cfg) => cfg.DiscardFaultedMessages());
            configureBus(x);
        });

        if (env.IsDevelopment())
            ConfigureOpenTelemetry(services, config, active);

        foreach (var svc in active)
            svc.Configure(services, config, redisConn, env);
    }

    // Shared with OpenTelemetry tracing/metrics resource attribution — every telemetry signal
    // reports under the same per-process service.name so the Aspire dashboard can tell the four
    // FplBot services apart even though they're all built from one assembly.
    public static string GetOtelServiceName(IEnumerable<IFplBotService> active) =>
        string.Join("+", active.Select(s => s.ServiceType));

    private static void ConfigureSerilog(HostBuilderContext ctx, LoggerConfiguration lc, List<IFplBotService> active)
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
          .Enrich.WithCorrelationId()
          .Enrich.WithCorrelationIdHeader()
          .WriteTo.Console(
              outputTemplate: "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}",
              theme: ConsoleTheme.None);

        if (ctx.HostingEnvironment.IsDevelopment())
        {
            Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine($"[Serilog SelfLog] {msg}"));
            lc.WriteTo.OpenTelemetry(o =>
            {
                o.Endpoint = ctx.Configuration["OTLP_DASHBOARD_ENDPOINT"];
                o.Protocol = OtlpProtocol.Grpc;
                o.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = GetOtelServiceName(active)
                };
            });
        }
    }

    private static void ConfigureOpenTelemetry(IServiceCollection services, IConfiguration config, List<IFplBotService> active)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(GetOtelServiceName(active)))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(DiagnosticHeaders.DefaultListenerName)
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(config["OTLP_DASHBOARD_ENDPOINT"]!);
                    o.Protocol = OtlpExportProtocol.Grpc;
                }));
    }

    private static void ConfigureCommon(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redisConn)
    {
        services.AddSingleton<IConnectionMultiplexer>(redisConn);
        services.AddSingleton(redisConn);
        services.AddStackExchangeRedisCache(o => o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redisConn));
        services.AddReducedHttpClientFactoryLogging();
        services.AddFplApiClient(config);
    }

    private static void ConfigureAzureServiceBus(IBusRegistrationConfigurator cfg, IConfiguration config)
    {
        cfg.UsingAzureServiceBus((ctx, bus) =>
        {
            var connectionString = config["ASB_CONNECTIONSTRING"]
                                   ?? throw new InvalidOperationException(
                                       "Service bus connection string not configured. Set ConnectionStrings__servicebus or ASB_CONNECTIONSTRING.");
            bus.Host(connectionString);
            bus.UseServiceBusMessageScheduler();
            bus.DefaultMessageTimeToLive = TimeSpan.FromHours(2);
            bus.ConfigureEndpoints(ctx);
        });
    }

    private static ConnectionMultiplexer BuildRedisConnection(IConfiguration config)
    {
        var connectionString = config["REDIS_URL"];
        if (connectionString == null)
            throw new InvalidOperationException("Redis connection string not configured. Set REDIS_URL.");

        if (connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectionMultiplexer.Connect(ParseRedisUrl(connectionString));
        }

        return ConnectionMultiplexer.Connect(connectionString);
    }

    private static ConfigurationOptions ParseRedisUrl(string redisUrl)
    {
        var uri = new Uri(redisUrl);
        var userInfo = uri.UserInfo.Split(':');
        var host = uri.Host + ":" + uri.Port;
        var options = new ConfigurationOptions
        {
            Password = userInfo.Length > 1 ? userInfo[1] : null,
            EndPoints = { host },
            Ssl = redisUrl.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase),
            SslClientAuthenticationOptions = _ => new SslClientAuthenticationOptions
            {
                TargetHost = uri.Host,
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            }
        };
        if (!string.IsNullOrEmpty(userInfo[0]))
            options.User = userInfo[0];
        return options;
    }
}
