using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client;
using Slackbot.Net.Workers.Handlers;
using Slackbot.Net.Workers.Publishers;
using SlackConnector.Models;

namespace FplBot.ConsoleApps.Handlers
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
            name = name.ToLower();

            var bootStrap = await _fplClient.GetBootstrap();

            var matchingPlayers = bootStrap.Elements.Where((p) => p.FirstName.ToLower().Contains(name) || p.LastName.ToLower().Contains(name));

            var textToSend = "";
            if (!matchingPlayers.Any())
            {
                textToSend = $"Fant ikke {name}";
            }
            else
            {
                textToSend = matchingPlayers.Any() ? string.Join("\n", matchingPlayers) : "";
            }
            
            foreach (var p in _publishers)
            {
                await p.Publish(new Notification
                {
                    Recipient = message.ChatHub.Id,
                    Msg = textToSend
                });
            }

            if (string.IsNullOrEmpty(textToSend))
            {
                return new HandleResponse("Not found");
 
            }
            return new HandleResponse(textToSend);
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