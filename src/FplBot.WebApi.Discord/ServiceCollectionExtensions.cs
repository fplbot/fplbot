using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.HttpClients;
using FplBot.Data.Discord;
using FplBot.Discord.Data;
using FplBot.Discord.Handlers.SlashCommands;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FplBot.Discord;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFplBotDiscordWebEndpoints(this IServiceCollection services, IConfiguration config,
        ConnectionMultiplexer connection)
    {
        services.AddSingleton<DiscordSlashCommandsEnsurer>();
        services.AddDiscordHttpClient(c =>
        {
            c.DiscordApplicationId = config["DiscordAppId"];
            c.DiscordAppToken = config["DISCORD_TOKEN"];
        });
        services.Configure<DiscordRedisOptions>(config);

        services.TryAddSingleton<IConnectionMultiplexer>(connection);
        services.AddSingleton<IGuildRepository, DiscordGuildRepository>();

        services.AddDiscordBotEvents<DiscordGuildStore>()
            .AddSlashCommandHandler<HelpSlashCommandHandler>()
            .AddSlashCommandHandler<FollowSlashCommandHandler>()
            .AddSlashCommandHandler<AddSubscriptionSlashCommandHandler>()
            .AddSlashCommandHandler<RemoveSubscriptionSlashCommandHandler>();
        return services;
    }
}
