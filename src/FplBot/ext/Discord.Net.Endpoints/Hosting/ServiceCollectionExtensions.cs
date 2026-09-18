using Microsoft.Extensions.DependencyInjection;

namespace Discord.Net.Endpoints.Hosting;

public static class ServiceCollectionExtensions
{
    public static IDiscordbotEventsBuilder AddDiscordBotEvents<T>(this IServiceCollection services) where T : class, IGuildInstallationHandler
    {
        services.AddScoped<IGuildInstallationHandler, T>();
        return new DiscordbotEventsBuilder(services);
    }

    internal const string TokenExchangeHttpClient = "Discord.Net.Endpoints.TokenExchange";

    public static IServiceCollection AddDiscordBotDistribution(this IServiceCollection services, Action<DiscordOAuthOptions> action)
    {
        services.Configure<DiscordOAuthOptions>(action);
        services.AddHttpClient(TokenExchangeHttpClient, c => c.BaseAddress = new Uri("https://discord.com/api/"));
        return services;
    }
}
