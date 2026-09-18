using System.Collections.Concurrent;
using AlmostServiceBus.TestHost;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using FplBot.WebApi.Admin;
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

    private IServiceScope _managerScope = null!;
    public IServiceProvider Services => _managerScope.ServiceProvider;

    public AdminErrorQueueService Service => Services.GetRequiredService<AdminErrorQueueService>();
    public AdminErrorQueueJobRunner Jobs => Services.GetRequiredService<AdminErrorQueueJobRunner>();
    public ServiceBusAdministrationClient AdminClient => Services.GetRequiredService<ServiceBusAdministrationClient>();
    public ServiceBusClient BusClient => Services.GetRequiredService<ServiceBusClient>();
    public IPublishEndpoint Publisher => Services.GetRequiredService<IPublishEndpoint>();

    // Every test in this collection that publishes PoisonTestMessage shares the SAME error queue
    // (AlwaysFaultsHandler_error) — drain your own message via this helper (or via a real
    // discard/retry/purge call) before the test ends, and always identify "your" message by its
    // `key`/body content, never by raw queue length.
    public async Task DrainMatchingAsync(string queue, string bodyContains, int maxMessages = 50)
    {
        await using var receiver = BusClient.CreateReceiver(queue);
        var received = await receiver.ReceiveMessagesAsync(maxMessages, TimeSpan.FromSeconds(2));
        foreach (var m in received)
        {
            if (m.Body.ToString().Contains(bodyContains, StringComparison.Ordinal))
                await receiver.CompleteMessageAsync(m);
            else
                await receiver.AbandonMessageAsync(m);
        }
    }

    // Mutating admin endpoints answer 202 and finish the work in the background, so a test that
    // asserts on the outcome has to wait for the job rather than for the HTTP response.
    public async Task<ErrorQueueJob> WaitForJobAsync(Guid jobId, int attempts = 60)
    {
        for (var i = 0; i < attempts; i++)
        {
            var job = Jobs.Get(jobId);
            if (job is { Status: ErrorQueueJobStatus.Succeeded or ErrorQueueJobStatus.Failed })
                return job;
            await Task.Delay(250);
        }

        throw new TimeoutException($"Job {jobId} did not finish in time.");
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
                services.AddSingleton<AdminErrorQueueService>();
                services.AddSingleton<AdminErrorQueueJobRunner>();
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<AlwaysFaultsHandler>();
                    // Deliberately NOT calling DiscardFaultedMessages() — matches the corrected
                    // production config (Step 1), so faults land in AlwaysFaultsHandler_error.
                    x.UsingAzureServiceBus((ctx, cfg) =>
                    {
                        cfg.Host(_emulator.ConnectionString);
                        cfg.ConfigureEndpoints(ctx);
                    });
                });
            });

        _host = hostBuilder.Build();
        _managerScope = _host.Services.CreateScope();
        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _managerScope.Dispose();
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
