using FplBot.Hosting;
using FplBot.WebApi.Infrastructure;
using MassTransit;
using Serilog;
using StackExchange.Redis;

namespace FplBot.Services.WebApi;

public class WebApiService : IFplBotService
{
    public FplBotService ServiceType => FplBotService.WebApi;

    public async Task RunHostAsync(string[] args, List<IFplBotService> allActive)
    {
        var builder = WebApplication.CreateBuilder(args);
        FplBotApplication.AddLocalUserSecrets(builder.Configuration, builder.Environment);
        builder.Host.UseSerilog((ctx, lc) => FplBotApplication.ConfigureSerilog(ctx, lc, allActive));
        var port = Environment.GetEnvironmentVariable("PORT") ?? "1337";
        // Slack requires OAuth redirect_uris to be https — even for localhost. In dev, serve
        // https on localhost using the trusted ASP.NET Core dev cert (`dotnet dev-certs https
        // --trust`) — the cert is issued for CN=localhost, so bind that host specifically
        // rather than "+". In prod (Heroku/containers), TLS is terminated at the platform
        // router and the app must stay reachable on all interfaces, so keep "+" and http.
        builder.WebHost.UseUrls(builder.Environment.IsLocal()
            ? $"https://localhost:{port}"
            : $"http://+:{port}");

        var redisConn = FplBotApplication.BuildRedisConnection(builder.Configuration);
        FplBotApplication.ConfigureServices(builder.Services, builder.Configuration, redisConn, builder.Environment, allActive,
            cfg => FplBotApplication.ConfigureAzureServiceBus(cfg, builder.Configuration));

        var app = builder.Build();
        foreach (var svc in allActive)
            svc.ConfigureApp(app);

        FplBotApplication.LogStartup(app, allActive);
        await app.RunAsync();
    }

    public void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.ConfigureWebApp(config, env, redis);
    }

    public void AddConsumers(IBusRegistrationConfigurator cfg) { }

    public void ConfigureApp(WebApplication app) => app.UseWebApp();
}
