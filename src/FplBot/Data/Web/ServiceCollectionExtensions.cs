namespace FplBot.Data.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebPushSubscribers(this IServiceCollection services)
    {
        services.AddSingleton<IWebPushSubscriberRepository, WebPushSubscriberRepository>();
        return services;
    }
}
