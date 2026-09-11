using Discord.Net.HttpClients;
using FplBot.Config;
using FplBot.Data.Discord;

namespace FplBot.EventHandlers.Discord;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDiscordServices(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.Configure<RedisOptions>(config);
        services.AddSingleton<IGuildRepository, DiscordGuildRepository>();
        services.AddDiscordHttpClient(c =>
        {
            c.DiscordApplicationId = config["DiscordAppId"] ?? string.Empty;
            c.DiscordAppToken = config["DISCORD_TOKEN"] ?? string.Empty;
        });
        if (!env.IsDevelopment())
        {
            services.AddTransient<IDiscordClient>(sp => sp.GetRequiredService<DiscordClient>());
        }

        IServiceCollection temp = services;
        services.AddOptions<DiscordClientOptions>()
            .ValidateWithFluentValidation(new DiscordClientOptionsValidator())
            .ValidateOnStart();
        return services;
    }

}
