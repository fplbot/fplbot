using System.Net.Http;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http.Configurations;

namespace Slackbot.Net.SlackClients.Http
{
    public class SlackClientBuilder : ISlackClientBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _factory;

        public SlackClientBuilder(ILoggerFactory loggerFactory, IHttpClientFactory factory)
        {
            _loggerFactory = loggerFactory;
            _factory = factory;
        }

        public ISlackClient Build(string token)
        {
            var c = _factory.CreateClient();
            CommonHttpClientConfiguration.ConfigureHttpClient(c, token);
            return new SlackClient(c,_loggerFactory.CreateLogger<ISlackClient>());
        }
    }
}