using Discord.Net.Endpoints.Hosting;
using Discord.Net.HttpClients;
using FplBot.Config;
using FplBot.Data.Discord;
using FplBot.Discord.Handlers.SlashCommands;
using FplBot.Services.WebApi.Discord.Handlers.Reactors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FplBot.Discord;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFplBotDiscordWebEndpoints(this IServiceCollection services, IConfiguration config,
        ConnectionMultiplexer connection, IHostEnvironment env)
    {
        services.AddSingleton<DiscordSlashCommandsEnsurer>();
        services.AddSingleton<ChannelDeliveryProbe>();
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

        services.Configure<RedisOptions>(config);

        services.TryAddSingleton<IConnectionMultiplexer>(connection);
        services.AddSingleton<IGuildRepository, DiscordGuildRepository>();

        services.AddDiscordBotEvents<DiscordNetInstallationBridge>()
            .AddSlashCommandHandler<HelpSlashCommandHandler>()
            .AddSlashCommandHandler<FollowSlashCommandHandler>()
            .AddSlashCommandHandler<AddSubscriptionSlashCommandHandler>()
            .AddSlashCommandHandler<RemoveSubscriptionSlashCommandHandler>()
            .AddSlashCommandHandler<PingSlashCommandHandler>();
        services.AddOptions<DiscordClientOptions>()
            .ValidateWithFluentValidation(new DiscordClientOptionsValidator())
            .ValidateOnStart();
        return services;
    }
}
