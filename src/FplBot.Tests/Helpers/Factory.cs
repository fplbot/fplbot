using System.Net.Http;
using FplBot.ConsoleApps;
using FplBot.ConsoleApps.Clients;

namespace FplBot.Tests.Helpers
{
    public class Factory
    {
        public static FplClient CreateClient()
        {
            var httpClient = new HttpClient(new FplClientHttpClientHandler());
            FplClientOptionsConfigurator.SetupFplClient(httpClient);
            return new FplClient(httpClient);
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