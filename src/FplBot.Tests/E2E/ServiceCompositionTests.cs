using FplBot.Hosting;
using FplBot.Services.EventHandlers;
using FplBot.Services.EventPublishers;
using FplBot.Services.SearchIndexer;
using FplBot.Services.WebApi;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FplBot.Tests.E2E;

public class ServiceCompositionFixture : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("redis:latest")
        .WithReuse(true)
        .WithLabel("reuse-id", "service-composition")
        .Build();

    public ConnectionMultiplexer Multiplexer { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _redis.StartAsync();
        var connStr = _redis.GetConnectionString();
        Multiplexer = await ConnectionMultiplexer.ConnectAsync(connStr + ",allowAdmin=true");
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["REDIS_URL"] = $"redis://user:pass@{connStr}",
                ["OTEL_ENABLED"] = "false"
            })
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await Multiplexer.DisposeAsync();
        await _redis.DisposeAsync();
    }
}

public class ServiceCompositionTests(ServiceCompositionFixture fixture) : IClassFixture<ServiceCompositionFixture>
{
    [Theory]
    [InlineData(FplBotService.WebApi)]
    [InlineData(FplBotService.EventHandlers)]
    [InlineData(FplBotService.EventPublishers)]
    [InlineData(FplBotService.SearchIndexer)]
    public void EveryServiceCanBeComposedOnItsOwn(FplBotService serviceType)
    {
        using var app = BuildAlone(serviceType, out _);

        Assert.NotNull(app.Services);
    }

    [Theory]
    [InlineData(FplBotService.WebApi)]
    [InlineData(FplBotService.EventHandlers)]
    [InlineData(FplBotService.EventPublishers)]
    [InlineData(FplBotService.SearchIndexer)]
    public void EveryServiceResolvesEverythingItRegistersOnItsOwn(FplBotService serviceType)
    {
        using var app = BuildAlone(serviceType, out var registered);
        using var scope = app.Services.CreateScope();

        var unresolvable = registered
            .Select(t => (Type: t, Error: Resolve(scope.ServiceProvider, t)))
            .Where(x => x.Error != null)
            .ToList();

        Assert.True(unresolvable.Count == 0,
            $"{serviceType} cannot resolve on its own:\n" +
            string.Join("\n", unresolvable.Select(x => $"  {x.Type.FullName}: {x.Error}")));
    }

    private WebApplication BuildAlone(FplBotService serviceType, out List<Type> registered)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.None);
        builder.Configuration.AddConfiguration(fixture.Configuration);

        List<IFplBotService> alone = [Create(serviceType)];
        FplBotApplication.ConfigureServices(builder.Services, fixture.Configuration, fixture.Multiplexer,
            builder.Environment, alone, cfg => cfg.UsingInMemory((ctx, c) => c.ConfigureEndpoints(ctx)));

        registered =
        [
            .. builder.Services
                .Select(d => d.ServiceType)
                .Where(t => !t.ContainsGenericParameters)
                .Distinct()
        ];

        return builder.Build();
    }

    private static IFplBotService Create(FplBotService serviceType) => serviceType switch
    {
        FplBotService.WebApi => new WebApiService(),
        FplBotService.EventHandlers => new EventHandlersService(),
        FplBotService.EventPublishers => new EventPublishersService(),
        FplBotService.SearchIndexer => new SearchIndexerService(),
        _ => throw new ArgumentOutOfRangeException(nameof(serviceType))
    };

    private static string? Resolve(IServiceProvider provider, Type serviceType)
    {
        try
        {
            provider.GetRequiredService(serviceType);
            return null;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }
}
