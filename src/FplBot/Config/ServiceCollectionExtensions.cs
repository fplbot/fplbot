namespace FplBot.Config;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ReRegisterAsScoped<TService, TImpl>(this IServiceCollection services)
        where TImpl : class, TService
        where TService : class
    {
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(TService) &&
            d.ImplementationType == typeof(TImpl));
        if (descriptor != null)
            services.Remove(descriptor);
        return services.AddScoped<TService, TImpl>();
    }
}
