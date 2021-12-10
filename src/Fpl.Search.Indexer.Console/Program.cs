using Fpl.Search;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.UseMinimalHttpLogger();
        var redisOptions = HerokuRedisConfigParser.ConfigurationOptions(Environment.GetEnvironmentVariable("REDIS_URL"));
        services.AddStackExchangeRedisCache(o =>
        {
            o.ConfigurationOptions = redisOptions;
        });
        services.AddFplApiClient(ctx.Configuration);

        var conn =  ConnectionMultiplexer.Connect(redisOptions);
        services.AddVerifiedEntries(ctx.Configuration);
        services.AddRecurringIndexer(ctx.Configuration, conn);
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
