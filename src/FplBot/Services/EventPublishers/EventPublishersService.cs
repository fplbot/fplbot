using FplBot.Hosting;
using StackExchange.Redis;

namespace FplBot.Services.EventPublishers;

public class EventPublishersService : WorkerFplBotService
{
    public override FplBotService ServiceType => FplBotService.EventPublishers;

    public override void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.AddFplWorkers();
    }
}
