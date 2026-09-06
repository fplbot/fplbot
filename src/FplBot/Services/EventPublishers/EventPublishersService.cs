using FplBot.Hosting;
using StackExchange.Redis;

namespace FplBot.Services.EventPublishers;

public class EventPublishersService : IFplBotService
{
    public FplBotService ServiceType => FplBotService.EventPublishers;

    public void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.AddFplWorkers();
    }
}
