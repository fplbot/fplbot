using FplBot.ConsoleApps;
using FplBot.ConsoleApps.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Helpers
{
    public class Factory
    {
        public static IFplClient CreateClient()
        {
            var config = new ConfigurationBuilder();
            config.AddJsonFile("appsettings.Local.json");
            config.AddEnvironmentVariables();
            
            var configuration = config.Build();
            
            var services = new ServiceCollection();
            services.AddHttpClient<IFplClient, FplClient>();
            services.AddSingleton<FplHttpHandler>();
            services.Configure<FplApiClientOptions>(configuration.GetSection("fpl"));
            
            services.ConfigureOptions<FplClientOptionsConfigurator>();
            var provider = services.BuildServiceProvider();
            return provider.GetService<IFplClient>();
        }

        public static FplCommandHandler CreateFplHandler()
        {
            return new FplCommandHandler(new[] { new DummyPublisher() }, CreateClient());
        }
        
        public static FplPlayerCommandHandler CreatePlayerHandler()
        {
            return new FplPlayerCommandHandler(new[] { new DummyPublisher() }, CreateClient());
        }
    }
}