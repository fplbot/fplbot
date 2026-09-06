using MassTransit;
using StackExchange.Redis;

namespace FplBot.Hosting;

public interface IFplBotService
{
    FplBotService ServiceType { get; }
    void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env);
    void ConfigureMassTransit(IBusRegistrationConfigurator cfg) { }
    void ConfigureApp(WebApplication app) { }
}
