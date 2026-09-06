namespace Discord.Net.Endpoints.Hosting;

internal class DiscordbotEventsBuilder(IServiceCollection services) : IDiscordbotEventsBuilder
{
    public IDiscordbotEventsBuilder AddSlashCommandHandler<T>() where T: class, ISlashCommandHandler
    {
        services.AddSingleton<ISlashCommandHandler, T>();
        return this;
    }
}
