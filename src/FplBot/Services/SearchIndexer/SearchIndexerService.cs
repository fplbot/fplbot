using FplBot.Hosting;
using StackExchange.Redis;

namespace FplBot.Services.SearchIndexer;

public class SearchIndexerService : IFplBotService
{
    public FplBotService ServiceType => FplBotService.SearchIndexer;

    public void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.AddRecurringIndexer(config, redis);
    }
}
