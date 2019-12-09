using System;
using FplBot.ConsoleApps.Clients;
using SlackConnector.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Workers.Handlers;
using Slackbot.Net.Workers.Publishers;

namespace FplBot.ConsoleApps
{
    public class FplPlayerCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IFplClient _fplClient;

        public FplPlayerCommandHandler(IEnumerable<IPublisher> publishers, IFplClient fplClient)
        {
            _publishers = publishers;
            _fplClient = fplClient;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var replacements = new[]{
                new {Find="@fplbot", Replace=""},
                new {Find="player", Replace=""},
                new {Find="<@UREFQD887>", Replace=""} // @fplbot-userid
            };

            var name = message.Text;

            foreach (var set in replacements)
            {
                name = name.Replace(set.Find, set.Replace).Trim();
            }

            var matchingPlayers = await _fplClient.GetAllFplDataForPlayer(name);

            var textToSend = matchingPlayers;
            if (string.IsNullOrEmpty(matchingPlayers))
                textToSend = $"Fant ikke {name}";
            
            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = textToSend
                });
            }

            if (string.IsNullOrEmpty(matchingPlayers))
            {
                return new HandleResponse("Not found");
 
            }
            return new HandleResponse(matchingPlayers);
        }

        public bool ShouldHandle(SlackMessage message)
        {
            return message.MentionsBot && message.Text.Contains("player");
        }

        public Tuple<string, string> GetHelpDescription()
        {
            return new Tuple<string, string>("player {navn}", "Henter info om spiller som matcher navn");
        }

        public bool ShouldShowInHelp => true;
    }
}