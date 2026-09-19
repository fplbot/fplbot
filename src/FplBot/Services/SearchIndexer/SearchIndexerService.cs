using FplBot.EventHandlers;
using FplBot.Hosting;
using MassTransit;
using StackExchange.Redis;

namespace FplBot.Services.SearchIndexer;

public class SearchIndexerService : WorkerFplBotService
{
    public override FplBotService ServiceType => FplBotService.SearchIndexer;

    public override void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.AddRecurringIndexer(config, redis);
    }

    public override void AddConsumers(IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<IndexQueryCommandHandler>();
    }
}
