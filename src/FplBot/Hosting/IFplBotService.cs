using MassTransit;
using StackExchange.Redis;

namespace FplBot.Hosting;

public interface IFplBotService
{
    FplBotService ServiceType { get; }
    Task RunHostAsync(string[] args, List<IFplBotService> allActive);
    void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env);
    void AddConsumers(IBusRegistrationConfigurator cfg);
    void ConfigureApp(WebApplication app);
}
