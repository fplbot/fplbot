using FplBot.EventHandlers;
using FplBot.Hosting;
using MassTransit;
using StackExchange.Redis;

namespace FplBot.Services.SearchIndexer;

public class SearchIndexerService : IFplBotService
{
    public FplBotService ServiceType => FplBotService.SearchIndexer;

    public void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.AddRecurringIndexer(config, redis);
    }

    public void ConfigureMassTransit(IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<IndexQueryCommandHandler>();
    }
}
