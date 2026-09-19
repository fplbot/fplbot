using MassTransit;
using Serilog;
using StackExchange.Redis;

namespace FplBot.Hosting;

public abstract class WorkerFplBotService : IFplBotService
{
    public abstract FplBotService ServiceType { get; }

    public abstract void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env);

    public virtual void AddConsumers(IBusRegistrationConfigurator cfg) { }

    public virtual void ConfigureApp(WebApplication app) { }

    public async Task RunHostAsync(string[] args, List<IFplBotService> allActive)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, config) => FplBotApplication.AddLocalUserSecrets(config, ctx.HostingEnvironment))
            .UseSerilog((ctx, lc) => FplBotApplication.ConfigureSerilog(ctx, lc, allActive))
            .ConfigureServices((ctx, services) =>
            {
                var redisConn = FplBotApplication.BuildRedisConnection(ctx.Configuration);
                FplBotApplication.ConfigureServices(services, ctx.Configuration, redisConn, ctx.HostingEnvironment, allActive,
                    cfg => FplBotApplication.ConfigureAzureServiceBus(cfg, ctx.Configuration));
            })
            .Build();

        FplBotApplication.LogStartup(host, allActive);
        await host.RunAsync();
    }
}
