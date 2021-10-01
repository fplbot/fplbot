using FplBot.VerifiedEntries;
using FplBot.VerifiedEntries.Data;
using FplBot.VerifiedEntries.Data.Abstractions;
using FplBot.VerifiedEntries.Data.Repositories;
using FplBot.VerifiedEntries.Helpers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;


// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionFplBotExtensions
    {
        public static IServiceCollection AddVerifiedEntries(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<VerifiedRedisOptions>(config);

            services.AddSingleton<ConnectionMultiplexer>(c =>
            {
                var opts = c.GetService<IOptions<VerifiedRedisOptions>>().Value;
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
            services.AddSingleton<SelfOwnerShipCalculator>();
            services.AddMediatR(typeof(FplEventHandlers));
            return services;
        }
    }
}
