using Discord.Net.Endpoints.Hosting;
using Discord.Net.HttpClients;
using FplBot.Config;
using FplBot.Data.Discord;
using FplBot.Discord.Data;
using FplBot.Discord.Handlers.SlashCommands;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FplBot.Discord;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFplBotDiscordWebEndpoints(this IServiceCollection services, IConfiguration config,
        ConnectionMultiplexer connection, IHostEnvironment env)
    {
        services.AddSingleton<DiscordSlashCommandsEnsurer>();
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
        services.Configure<RedisOptions>(config);

        services.TryAddSingleton<IConnectionMultiplexer>(connection);
        services.AddSingleton<IGuildRepository, DiscordGuildRepository>();

        services.AddDiscordBotEvents<DiscordGuildStore>()
            .AddSlashCommandHandler<HelpSlashCommandHandler>()
            .AddSlashCommandHandler<FollowSlashCommandHandler>()
            .AddSlashCommandHandler<AddSubscriptionSlashCommandHandler>()
            .AddSlashCommandHandler<RemoveSubscriptionSlashCommandHandler>();
        services.AddOptions<DiscordClientOptions>()
            .ValidateWithFluentValidation(new DiscordClientOptionsValidator())
            .ValidateOnStart();
        return services;
    }
}
