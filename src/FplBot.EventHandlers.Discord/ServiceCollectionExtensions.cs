using Discord.Net.HttpClients;
using FplBot.Data.Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.EventHandlers.Discord;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<DiscordRedisOptions>(config);
        services.AddSingleton<IGuildRepository, DiscordGuildRepository>();
        services.AddDiscordHttpClient(c =>
        {
            c.DiscordApplicationId = config["DiscordAppId"];
            c.DiscordAppToken = config["DISCORD_TOKEN"];
        });
        return services;
    }

}
