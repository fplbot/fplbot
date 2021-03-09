using Fpl.Data.Abstractions;
using Fpl.Data.Repositories.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Fpl.Data
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisData(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<RedisOptions>(config);

            services.AddSingleton<ConnectionMultiplexer>(c =>
            {
                var opts = c.GetService<IOptions<RedisOptions>>().Value;
                var options = new ConfigurationOptions
                {
                    ClientName = opts.GetRedisUsername,
                    Password = opts.GetRedisPassword,
                    EndPoints = {opts.GetRedisServerHostAndPort}
                };
                return ConnectionMultiplexer.Connect(options);
            });

            services.AddSingleton<IVerifiedEntriesRepository, VerifiedEntriesRepository>();
            services.AddSingleton<IVerifiedPLEntriesRepository, VerifiedPLEntriesRepository>();
            services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
            services.AddSingleton<IIndexBookmarkProvider, LeagueIndexRedisBookmarkProvider>();

            return services;
        }
    }
}
