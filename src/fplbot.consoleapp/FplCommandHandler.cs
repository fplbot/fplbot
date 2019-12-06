using System.Collections.Generic;
using System.Threading.Tasks;
using Slackbot.Net.Handlers;
using Slackbot.Net.Publishers;
using SlackConnector.Models;

namespace fplbot.consoleapp
{
    public class FplCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;

        public FplCommandHandler(IEnumerable<IPublisher> publishers)
        {
            _publishers = publishers;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    BotName = "fpl",
                    Channel = "#fplbot",
                    Msg = $"fpl pong: {message.Text}",
                    IconEmoji = ":santa:"
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