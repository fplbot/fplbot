using CronBackgroundServices;
using Fpl.Search;
using FplBot.Core.RecurringActions;
using StackExchange.Redis;

namespace FplBot.Services.SearchIndexer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRecurringIndexer(this IServiceCollection services, IConfiguration config, IConnectionMultiplexer conn)
    {
        services.AddIndexingServices(config, conn);
        services.AddRecurrer<IndexerRecurringAction>();
        return services;
    }
}
