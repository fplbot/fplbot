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
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplPlayerCommandHandler : IHandleMessages
    {
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly ISlackClient _slackClient;


        private readonly BotDetails _botDetails;

        public FplPlayerCommandHandler(ISlackClient slackClient, IPlayerClient playerClient , ITeamsClient teamsClient, BotDetails botDetails)
        {
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _botDetails = botDetails;
            _slackClient = slackClient;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var name = ParsePlayerFromInput(message);

            var allPlayers = await _playerClient.GetAllPlayers();

            var matchingPlayers = FindMatchingPlayer(allPlayers, name);
            
            if (!matchingPlayers.Any())
            {
                await _slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = message.ChatHub.Id,
                    Text = $"Fant ikke {name}"
                });
                return new HandleResponse($"Found no matching player for {name}");
            }
            
            var teams = await _teamsClient.GetAllTeams();
            foreach(var p in matchingPlayers)
            {
                await _slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = message.ChatHub.Id,
                    Blocks = Formatter.GetPlayerCard(p, teams)
                });
            }
            
            return new HandleResponse($"Found matching players for {name}");
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