using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Abstractions.Publishers;
using Slackbot.Net.Extensions.FplBot.Helpers;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    public class FplPlayerCommandHandler : IHandleMessages
    {
        private readonly IEnumerable<IPublisher> _publishers;
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly BotDetails _botDetails;

        public FplPlayerCommandHandler(IEnumerable<IPublisher> publishers, IPlayerClient playerClient , ITeamsClient teamsClient, BotDetails botDetails)
        {
            _publishers = publishers;
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _botDetails = botDetails;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var name = ParsePlayerFromInput(message);

            var allPlayers = await _playerClient.GetAllPlayers();

            var matchingPlayers = FindMatchingPlayer(allPlayers, name);
            
            var textToSend = "";
            if (!matchingPlayers.Any())
            {
                textToSend = $"Fant ikke {name}";
            }
            else
            {
                var teams = await _teamsClient.GetAllTeams();
                var players = matchingPlayers.Select(p => Formatter.GetPlayer(p, teams));

                textToSend = string.Join("\n\n", players);
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
            return players.Where((p) => p.FirstName.ToLower().Contains(name) || p.SecondName.ToLower().Contains(name) || p.WebName.ToLower().Contains(name));
        }

        private string ParsePlayerFromInput(SlackMessage message)
        {
            var replacements = new[]
            {
                new {Find = "@fplbot", Replace = ""},
                new {Find = "player", Replace = ""},
                new {Find = $"<@{_botDetails.Id}>", Replace = ""} // @fplbot-userid
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