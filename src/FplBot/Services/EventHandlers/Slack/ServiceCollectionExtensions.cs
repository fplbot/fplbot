using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using Slackbot.Net.SlackClients.Http.Extensions;

namespace FplBot.EventHandlers.Slack;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlackServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RedisOptions>(config);
        services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
        services.AddSlackClientBuilder();
        services.AddSingleton<SlackWorkSpacePublisher>();
        services.AddSingleton<ISlackWorkSpacePublisher, DevLoggingSlackWorkSpacePublisher>();
        return services;
    }

}
