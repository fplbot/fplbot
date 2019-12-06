using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fplbot.consoleapp.Clients;
using Newtonsoft.Json;
using Slackbot.Net.Handlers;
using Slackbot.Net.Publishers;
using SlackConnector.Models;

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
            var hest = await _fplClient.GetScoreBoard("579157");

            foreach (var p in _publishers)
            {

                await p.Publish(new Notification
                {
                    BotName = "fpl",
                    Channel = "#fplbot",
                    Msg = JsonConvert.SerializeObject(hest),
                    IconEmoji = ":santa:"
                });
            }

            return new HandleResponse("OK");
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return true;
            return message.MentionsBot && message.Text.Contains("fpl");
        }
    }
}