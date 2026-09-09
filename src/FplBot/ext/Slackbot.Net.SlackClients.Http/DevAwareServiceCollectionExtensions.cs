using Slackbot.Net.SlackClients.Http.Extensions;

namespace Slackbot.Net.SlackClients.Http;

public static class DevAwareServiceCollectionExtensions
{
    /// <summary>
    /// Registers ISlackClientBuilder the same way AddSlackClientBuilder does. In Development,
    /// wraps it so every ISlackClient it builds is dev-safe (see DevLoggingSlackClientBuilder);
    /// other environments get the real builder only.
    /// </summary>
    public static IServiceCollection AddDevAwareSlackClientBuilder(this IServiceCollection services, IHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            services.AddSlackClientBuilder();
            return services;
        }

        // AddSlackClientBuilder() also registers ISlackClientBuilder -> SlackClientBuilder, which
        // we'd immediately shadow below; register only what dev actually needs instead.
        services.AddHttpClient();
        services.AddSingleton<SlackClientBuilder>();
        services.AddSingleton<ISlackClientBuilder, DevLoggingSlackClientBuilder>();
        return services;
    }
}
