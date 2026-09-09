using Discord.Net.HttpClients;

namespace FplBot.Config;

public static class DiscordDevAwareClientExtensions
{
    /// <summary>
    /// AddDiscordHttpClient (ext/Discord.Net.HttpClients) always registers IDiscordClient as a
    /// DevLoggingDiscordClient, which itself checks IsDevelopment on every call. Call this right
    /// after AddDiscordHttpClient to skip that wrapper entirely outside Development, so IDiscordClient
    /// doesn't need to be registered as anything but the real client in other environments.
    ///
    /// The Slack equivalent (AddDevAwareSlackClientBuilder, ext/Slackbot.Net.SlackClients.Http)
    /// instead only registers its dev wrapper in the first place, never registering the wrapper
    /// at all outside Development. That approach isn't available here without editing
    /// ext/Discord.Net.HttpClients itself, so this overrides the registration after the fact.
    /// </summary>
    public static IServiceCollection UseRealDiscordClientOutsideDevelopment(this IServiceCollection services, IHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            services.AddTransient<IDiscordClient>(sp => sp.GetRequiredService<DiscordClient>());
        }
        return services;
    }
}
