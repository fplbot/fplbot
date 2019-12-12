using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Workers.Handlers;
using Slackbot.Net.Workers.Publishers;
using SlackConnector.Models;

namespace FplBot.ConsoleApps.Handlers
{
    public class FplPlayerCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IPlayerClient _globalSettingsClient;

        public FplPlayerCommandHandler(IEnumerable<IPublisher> publishers, IPlayerClient globalSettingsClient)
        {
            _publishers = publishers;
            _globalSettingsClient = globalSettingsClient;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var name = ParsePlayerFromInput(message);

            var allPlayers = await _globalSettingsClient.GetAllPlayers();

            var matchingPlayers = FindMatchingPlayer(allPlayers, name);
            
            var textToSend = "";
            if (!matchingPlayers.Any())
            {
                textToSend = $"Fant ikke {name}";
            }
            else
            {
                var names = matchingPlayers.Select(p => $"{p.FirstName} {p.SecondName}");
                textToSend = string.Join("\n", names);
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

        private static IEnumerable<Player> FindMatchingPlayer(IEnumerable<Player> players, string name)
        {
            return players.Where((p) => p.FirstName.ToLower().Contains(name) || p.SecondName.ToLower().Contains(name));
        }

        private static string ParsePlayerFromInput(SlackMessage message)
        {
            var replacements = new[]
            {
                new {Find = "@fplbot", Replace = ""},
                new {Find = "player", Replace = ""},
                new {Find = "<@UREFQD887>", Replace = ""} // @fplbot-userid
            };

            var name = message.Text;

            foreach (var set in replacements)
            {
                name = name.Replace(set.Find, set.Replace).Trim();
            }

            name = name.ToLower();
            return name;
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