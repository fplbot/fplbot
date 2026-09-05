using FplBot.Hosting;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
