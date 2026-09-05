using FplBot.Hosting;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
