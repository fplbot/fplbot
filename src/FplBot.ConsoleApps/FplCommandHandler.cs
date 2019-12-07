using FplBot.ConsoleApps.Clients;
using Slackbot.Net.Handlers;
using Slackbot.Net.Publishers;
using SlackConnector.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FplBot.ConsoleApps
{
    public class FplCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IFplClient _fplClient;

        public FplCommandHandler(IEnumerable<IPublisher> publishers, IFplClient fplClient)
        {
            _publishers = publishers;
            _fplClient = fplClient;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var standings = await _fplClient.GetStandings("579157");

            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    BotName = "fpl",
                    Channel = message.ChatHub.Id,
                    Msg = standings
                });
            }

            return new HandleResponse("OK");
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("fpl");
        }
    }
}