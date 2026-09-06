using FplBot.Hosting;
using FplBot.WebApi.Infrastructure;
using MassTransit;
using StackExchange.Redis;

namespace FplBot.Services.WebApi;

public class WebApiService : IFplBotService
{
    public FplBotService ServiceType => FplBotService.WebApi;

    public void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.ConfigureWebApp(config, env, redis);
    }

    public void ConfigureMassTransit(IBusRegistrationConfigurator cfg) { }

    public void ConfigureApp(WebApplication app) => app.UseWebApp();
}
