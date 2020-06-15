using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Slackbot.Net.Dynamic;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.WebApi.Controllers
{
    public interface IHandleAllEvents
    {
        Task Handle(EventWrapper eventWrapper);
    }

    class HandleAllEvents : IHandleAllEvents
    {
        private readonly ILogger<HandleAllEvents> _logger;
        private readonly ISlackClientService _client;

        public HandleAllEvents(ILogger<HandleAllEvents> logger, ISlackClientService client)
        {
            _logger = logger;
            _client = client;
        }
        
        public async Task Handle(EventWrapper eventWrapper)
        {
            _logger.LogInformation("PAYLOAD RECEIVED FROM EVENT API");
            var client = await _client.CreateClient(eventWrapper.Team_Id);
            await client.ChatPostMessage(eventWrapper.Event.Channel, $"Pong: {eventWrapper.Event.Text} ");
        }
    }
}