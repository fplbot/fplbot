using System.Collections.Concurrent;
using AlmostServiceBus.TestHost;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.E2E.Admin;

public class AdminErrorQueueFixture : IAsyncLifetime
{
    // Fixed and distinct from devenv's port 6000 (src/devenv.sh / FplBot.AppHost), so running
    // `dotnet test` alongside a locally running devenv never collides. ServiceBusAdministrationClient
    // only works against a fixed public port for this emulator (its management multiplexer on port
    // 5300 is only bound when a fixed port is requested), so every test needing admin/management
    // operations must share this one fixture instance — see AdminErrorQueueCollection below.
    private const int EmulatorPort = 16712;

    private readonly ServiceBusEmulatorFixture _emulator = new(EmulatorPort);
    private IHost _host = null!;

    // public AdminErrorQueueService Service => _host.Services.GetRequiredService<AdminErrorQueueService>();
    public ServiceBusAdministrationClient AdminClient => _host.Services.GetRequiredService<ServiceBusAdministrationClient>();
    public ServiceBusClient BusClient => _host.Services.GetRequiredService<ServiceBusClient>();
    public IPublishEndpoint Publisher => _host.Services.GetRequiredService<IPublishEndpoint>();

    // Every test in this collection publishes the SAME PoisonTestMessage type, so they all share
    // ONE fault topic (MassTransit provisions one fault topic per message TYPE, not per test).
    // Tests must drain their own message when done so the next test doesn't see stray leftovers.
    // Used only by tests — not a production code path, so it's fine to live on the fixture itself.
    public async Task DrainMatchingAsync(string topic, string subscription, string bodyContains, int maxMessages = 50)
    {
        await using var receiver = BusClient.CreateReceiver(topic, subscription);
        var received = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(2));
        foreach (var m in received)
        {
            if (m.Body.ToString().Contains(bodyContains, StringComparison.Ordinal))
                await receiver.CompleteMessageAsync(m);
            else
                await receiver.AbandonMessageAsync(m);
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _emulator.StartAsync();

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(b => b.AddConsole());
                services.AddSingleton(new ServiceBusAdministrationClient(_emulator.ConnectionString));
                services.AddSingleton(new ServiceBusClient(_emulator.ConnectionString));
                // services.AddSingleton<AdminErrorQueueService>();
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<AlwaysFaultsHandler>();
                    // Matches Hosting/FplBotApplication.cs's production bus config exactly — verified
                    // this has zero effect on the fault topic itself (only on whether a {queue}_error
                    // queue also gets created), but matching it keeps this fixture faithful to the
                    // real topology.
                    x.AddConfigureEndpointsCallback((_, cfg) => cfg.DiscardFaultedMessages());
                    x.UsingAzureServiceBus((ctx, cfg) =>
                    {
                        cfg.Host(_emulator.ConnectionString);
                        cfg.ConfigureEndpoints(ctx);
                    });
                });
            });

        _host = hostBuilder.Build();
        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        await _emulator.DisposeAsync();
    }
}

public record PoisonTestMessage(string Key, bool AlwaysFault);

public class AlwaysFaultsHandler : IConsumer<PoisonTestMessage>
{
    public static readonly ConcurrentDictionary<string, int> Attempts = new();

    public Task Consume(ConsumeContext<PoisonTestMessage> context)
    {
        var attempt = Attempts.AddOrUpdate(context.Message.Key, 1, (_, n) => n + 1);
        if (context.Message.AlwaysFault || attempt == 1)
            throw new InvalidOperationException($"Poison message faulted (attempt {attempt})");

        return Task.CompletedTask;
    }
}

[CollectionDefinition("AdminErrorQueue")]
public class AdminErrorQueueCollection : ICollectionFixture<AdminErrorQueueFixture>;
