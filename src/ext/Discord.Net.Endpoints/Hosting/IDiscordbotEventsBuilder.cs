namespace Discord.Net.Endpoints.Hosting;

public interface IDiscordbotEventsBuilder
{
    IDiscordbotEventsBuilder AddSlashCommandHandler<T>() where T: class, ISlashCommandHandler;
}
