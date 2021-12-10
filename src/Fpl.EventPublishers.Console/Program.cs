using Fpl.EventPublishers.Console;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

IHost host = Host.CreateDefaultBuilder(args)
    .UseMessaging()
    .ConfigureServices((context,services) =>
    {
        services.AddStackExchangeRedisCache(o =>
        {
            o.ConfigurationOptions = HerokuRedisConfigParser.ConfigurationOptions(Environment.GetEnvironmentVariable("REDIS_URL"));
        });
        services.AddFplApiClient(context.Configuration);
        services.AddFplWorkers();
        services.AddReducedHttpClientFactoryLogging();

    })
    .UseSerilog((hostingContext, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(hostingContext.Configuration)
            .WriteTo.Console(
                outputTemplate:
                "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                theme: ConsoleTheme.None))

    .Build();

await host.RunAsync();
