using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Controllers
{
    public interface IHandleAllEvents
    {
        Task Handle(string payload);
    }

    class HandleAllEvents : IHandleAllEvents
    {
        private readonly ILogger<HandleAllEvents> _logger;
        private readonly ISlackClientBuilder _client;

        public HandleAllEvents(ILogger<HandleAllEvents> logger, ISlackClientBuilder client)
        {
            _logger = logger;
            _client = client;
        }
        
        public Task Handle(string payload)
        {
            _logger.LogInformation(payload);
            return Task.CompletedTask;
        }
    }
}