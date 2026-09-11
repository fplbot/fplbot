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

        // No real SlackClientBuilder/HttpClient registered at all in Development — nothing
        // needs one, since DevLoggingSlackClientBuilder never constructs a real client.
        services.AddSingleton<ISlackClientBuilder, DevLoggingSlackClientBuilder>();
        return services;
    }
}
