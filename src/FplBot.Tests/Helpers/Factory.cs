using FplBot.ConsoleApps;
using FplBot.ConsoleApps.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace FplBot.Tests.Helpers
{
    public class Factory
    {
        public static IFplClient CreateClient(ITestOutputHelper logger = null)
        {
            var config = new ConfigurationBuilder();
            config.AddJsonFile("appsettings.Local.json", optional:true);
            config.AddEnvironmentVariables();
            var configuration = config.Build();
            
            var services = new ServiceCollection();
            services.AddFplApiClient(configuration.GetSection("fpl"));
            services.AddSingleton<ILogger<CookieFetcher>, TestLogger<CookieFetcher>>(s => new TestLogger<CookieFetcher>(logger));
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