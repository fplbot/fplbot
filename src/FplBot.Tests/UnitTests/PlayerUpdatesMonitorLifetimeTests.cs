using Fpl.Client.Models;
using Fpl.EventPublishers.RecurringActions;
using Fpl.EventPublishers.States;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FplBot.Tests.UnitTests;

// CronBackgroundServices 10.0.0 resolves a fresh scope, and therefore a fresh
// PlayerUpdatesRecurringAction instance, on every single cron tick (see its
// ServiceCollectionExtensions.AddRecurrer<T>: services.AddScoped<T>()). Diffing state that
// used to live directly on that action would silently reset every tick and never fire again.
// This test wires the DI graph the same way AddFplWorkers/AddRecurrer does and drives it
// through two separate scopes, the way the real cron host does, to prove the diff survives.
public class PlayerUpdatesMonitorLifetimeTests
{
    [Fact]
    public async Task PriceChangeIsDetectedAcrossFreshScopesPerTick()
    {
        var settingsClient = GlobalSettingsClientBuilder.Returning(
            new GlobalSettings
            {
                Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
                Players = [TestBuilder.Player()]
            },
            new GlobalSettings
            {
                Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
                Players = [TestBuilder.Player().WithCost(1)]
            });

        var messageSession = new TestPublishEndpoint();

        var services = new ServiceCollection();
        services.AddSingleton(settingsClient);
        services.AddSingleton<IPublishEndpoint>(messageSession);
        services.AddSingleton<ILogger<PlayerUpdatesMonitor>>(NullLogger<PlayerUpdatesMonitor>.Instance);
        services.AddSingleton<ILogger<PlayerUpdatesRecurringAction>>(NullLogger<PlayerUpdatesRecurringAction>.Instance);
        services.AddSingleton<PlayerUpdatesMonitor>();
        services.AddScoped<PlayerUpdatesRecurringAction>();
        await using var provider = services.BuildServiceProvider();

        // First tick: a fresh scope, seeds the baseline (mirrors the "Init state" branch).
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<PlayerUpdatesRecurringAction>().Process(CancellationToken.None);
        }

        // Second tick: a brand new scope and a brand new PlayerUpdatesRecurringAction instance,
        // exactly like the real cron host creates every two minutes.
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<PlayerUpdatesRecurringAction>().Process(CancellationToken.None);
        }

        Assert.Single(messageSession.PublishedMessages);
        Assert.IsType<PlayersPriceChanged>(messageSession.PublishedMessages[0].Message);
    }
}
