using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers.Slack;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlackServices(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        services.Configure<RedisOptions>(config);
        services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
        services.AddDevAwareSlackClientBuilder(env);
        services.AddSingleton<ISlackWorkSpacePublisher, SlackWorkSpacePublisher>();
        return services;
    }

}
