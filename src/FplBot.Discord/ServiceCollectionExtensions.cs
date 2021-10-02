using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Discord.Net.Endpoints.Hosting;
using Discord.Net.HttpClients;
using FplBot.Discord.Data;
using FplBot.Discord.Handlers.SlashCommands;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FplBot.Discord
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFplBotDiscord(this IServiceCollection services, IConfiguration config)
        {
            services.AddHostedService<DiscordSlashCommandsEnsurer>();

            services.AddDiscordHttpClient(c =>
            {
                c.DiscordApplicationId = config["DiscordAppId"];
                c.DiscordAppToken = config["DISCORD_TOKEN"];
            });
            services.Configure<DiscordRedisOptions>(config);

            services.AddSingleton<ConnectionMultiplexer>(c =>
            {
                var opts = c.GetService<IOptions<DiscordRedisOptions>>().Value;
                var options = new ConfigurationOptions
                {
                    ClientName = opts.GetRedisUsername,
                    Password = opts.GetRedisPassword,
                    EndPoints = {opts.GetRedisServerHostAndPort}
                };
                return ConnectionMultiplexer.Connect(options);
            });

            services.AddSingleton<IGuildRepository, DiscordGuildStore>();
            services.AddDiscordBotEvents<DiscordGuildStore>()
                .AddSlashCommandHandler<HelpSlashCommandHandler>()
                .AddSlashCommandHandler<FollowSlashCommandHandler>()
                .AddSlashCommandHandler<SubscriptionsSlashCommandHandler>();
            return services;
        }
    }
}
