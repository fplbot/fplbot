using Microsoft.Extensions.DependencyInjection;

namespace Discord.Net.Endpoints.Hosting
{
    internal class DiscordbotEventsBuilder : IDiscordbotEventsBuilder
    {
        private readonly IServiceCollection _services;

        public DiscordbotEventsBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public IDiscordbotEventsBuilder AddSlashCommandHandler<T>() where T: class, ISlashCommandHandler
        {
            _services.AddSingleton<ISlashCommandHandler, T>();
            return this;
        }
    }
}
