using Discord.Net.HttpClients;
using FplBot.Data;
using FplBot.Data.Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.EventHandlers.Discord;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RedisOptions>(config);
        services.AddSingleton<IGuildRepository, DiscordGuildRepository>();
        services.AddDiscordHttpClient(c =>
        {
            c.DiscordApplicationId = config["DiscordAppId"] ?? string.Empty;
            c.DiscordAppToken = config["DISCORD_TOKEN"] ?? string.Empty;
        });
        return services;
    }

}
