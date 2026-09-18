using FplBot.Config;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Integrations.Slack;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers.Slack;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlackServices(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.Configure<RedisOptions>(config);
        services.Configure<OAuthOptions>(c =>
        {
            c.CLIENT_ID = config["CLIENT_ID"];
            c.CLIENT_SECRET = config["CLIENT_SECRET"];
        });
        services.AddOptions<OAuthOptions>()
            .ValidateWithFluentValidation(new OAuthOptionsValidator())
            .ValidateOnStart();
        services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
        services.AddDevAwareSlackClientBuilder(env);
        services.AddSingleton<ISlackWorkSpacePublisher, SlackWorkSpacePublisher>();
        return services;
    }
}
