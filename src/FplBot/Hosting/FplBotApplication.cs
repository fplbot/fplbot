using System.Net.Security;
using FplBot.Services.EventHandlers;
using FplBot.Services.EventPublishers;
using FplBot.Services.SearchIndexer;
using FplBot.Services.WebApi;
using MassTransit;
using Serilog;
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
        builder.Host.UseSerilog(ConfigureSerilog);
        if (Environment.GetEnvironmentVariable("PORT") is { } port)
            builder.WebHost.UseUrls($"http://+:{port}");

        var redisConn = BuildRedisConnection(builder.Configuration);
        ConfigureCommon(builder.Services, builder.Configuration, redisConn);
        builder.Services.AddMassTransit(x =>
        {
            foreach (var svc in active)
                svc.ConfigureMassTransit(x);
            x.AddConfigureEndpointsCallback((_, cfg) => cfg.DiscardFaultedMessages());
            ConfigureAzureServiceBus(x, builder.Configuration);
        });

        foreach (var svc in active)
            svc.Configure(builder.Services, builder.Configuration, redisConn, builder.Environment);

        var app = builder.Build();
        foreach (var svc in active)
            svc.ConfigureApp(app);

        await app.RunAsync();
    }

    private static async Task RunAsWorkerHost(string[] args, List<IFplBotService> active)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(ConfigureSerilog)
            .ConfigureServices((ctx, services) =>
            {
                var redisConn = BuildRedisConnection(ctx.Configuration);
                ConfigureCommon(services, ctx.Configuration, redisConn);
                services.AddMassTransit(x =>
                {
                    foreach (var svc in active)
                        svc.ConfigureMassTransit(x);
                    x.AddConfigureEndpointsCallback((_, cfg) => cfg.DiscardFaultedMessages());
                    ConfigureAzureServiceBus(x, ctx.Configuration);
                });

                foreach (var svc in active)
                    svc.Configure(services, ctx.Configuration, redisConn, ctx.HostingEnvironment);
            })
            .Build();

        await host.RunAsync();
    }

    private static void ConfigureSerilog(HostBuilderContext ctx, LoggerConfiguration lc)
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
          .Enrich.WithCorrelationId()
          .Enrich.WithCorrelationIdHeader()
          .WriteTo.Console(
              outputTemplate: "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}",
              theme: ConsoleTheme.None);
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
