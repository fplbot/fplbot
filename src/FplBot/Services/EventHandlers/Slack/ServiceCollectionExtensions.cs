using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.SlackClients.Http.Extensions;

namespace FplBot.EventHandlers.Slack;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlackServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SlackRedisOptions>(config);
        services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
        services.AddSlackClientBuilder();
        services.AddSingleton<ISlackWorkSpacePublisher, SlackWorkSpacePublisher>();
        return services;
    }

}
