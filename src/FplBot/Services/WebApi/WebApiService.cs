using FplBot.Hosting;
using FplBot.WebApi.Handlers.Commands;
using FplBot.WebApi.Handlers.Events;
using FplBot.WebApi.Handlers.Sagas;
using FplBot.WebApi.Infrastructure;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace FplBot.Services.WebApi;

public class WebApiService : IFplBotService
{
    public FplBotService ServiceType => FplBotService.WebApi;

    public void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.ConfigureWebApp(config, env, redis);
    }

    public void ConfigureMassTransit(IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<AppInstalledHandler>();
        cfg.AddConsumer<IndexQueryCommandHandler>();
        cfg.AddConsumer<AggregatedSuggestionsHandler>();
        cfg.AddSagaStateMachine<ThrottleEntrySuggestionsSagaStateMachine, AcccumulatedSuggestionsSagaState>()
            .InMemoryRepository();
        cfg.AddSagaStateMachine<ThrottlePlSuggestionsSagaStateMachine, AcccumulatedPLSuggestionsSagaState>()
            .InMemoryRepository();
    }

    public void ConfigureApp(WebApplication app) => app.UseWebApp();
}
