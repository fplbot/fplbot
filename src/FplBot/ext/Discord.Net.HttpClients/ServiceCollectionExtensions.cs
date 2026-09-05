using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Discord.Net.HttpClients
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDiscordHttpClient(this IServiceCollection services, Action<DiscordClientOptions> optionsConfig)
        {
            services.Configure(optionsConfig);
            services.AddHttpClient<DiscordClient>((s,c) =>
            {
                var token = s.GetService<IOptions<DiscordClientOptions>>().Value.DiscordAppToken;
                c.BaseAddress = new Uri("https://discord.com/");
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
                c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("fplbot", "1"));
            });
            return services;
        }
    }
}
