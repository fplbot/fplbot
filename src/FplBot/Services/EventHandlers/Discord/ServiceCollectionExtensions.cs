using Discord.Net.HttpClients;
using FplBot.Config;
using FplBot.Data.Discord;
using FplBot.Integrations.Discord;

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
        if (env.IsDevelopment())
        {
            services.AddTransient<IDiscordClient>(sp =>
                new DevLoggingDiscordClient(
                    sp.GetRequiredService<DiscordClient>(),
                    sp.GetRequiredService<IHostEnvironment>(),
                    sp.GetRequiredService<ILogger<DevLoggingDiscordClient>>()));
        }
        else
        {
            services.AddTransient<IDiscordClient>(sp => sp.GetRequiredService<DiscordClient>());
        }

        var temp = services;
        services.AddOptions<DiscordClientOptions>()
            .ValidateWithFluentValidation(new DiscordClientOptionsValidator())
            .ValidateOnStart();
        return services;
    }
}
