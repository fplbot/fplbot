using FplBot.Hosting;
using StackExchange.Redis;

namespace FplBot.Services.SearchIndexer;

public class SearchIndexerService : WorkerFplBotService
{
    public override FplBotService ServiceType => FplBotService.SearchIndexer;

    public override void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.AddRecurringIndexer(config, redis);
    }
}
