using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplPlayerCommandHandler : IHandleMessages
    {
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly IMessageHelper _messageHelper;
        private readonly ISlackClient _slackClient;

        public FplPlayerCommandHandler(
            ISlackClient slackClient, 
            IPlayerClient playerClient, 
            ITeamsClient teamsClient, 
            IMessageHelper messageHelper)
        {
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _messageHelper = messageHelper;
            _slackClient = slackClient;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var allPlayersTask = _playerClient.GetAllPlayers();
            var teamsTask = _teamsClient.GetAllTeams();

            var name = ParsePlayerFromInput(message);

            var allPlayers = await allPlayersTask;
            var matchingPlayers = FindMatchingPlayer(allPlayers, name).ToArray();
            
            if (!matchingPlayers.Any())
            {
                await _slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = message.ChatHub.Id,
                    Text = $"Couldn't find {name}"
                });
                return new HandleResponse($"Found no matching player for {name}");
            }

            var teams = await teamsTask;
            foreach (var p in matchingPlayers)
            {
                await _slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = message.ChatHub.Id,
                    Blocks = Formatter.GetPlayerCard(p, teams)
                });
            }
            
            return new HandleResponse($"Found matching player(s) for {name}");
        }

        private static IEnumerable<Player> FindMatchingPlayer(IEnumerable<Player> players, string name)
        {
            return players.Where(p => p.FirstName.ToLower().Contains(name) || p.SecondName.ToLower().Contains(name) || p.WebName.ToLower().Contains(name));
        }

        private string ParsePlayerFromInput(SlackMessage message)
        {
            var name = _messageHelper.ExtractArgs(message.Text, "player {args}");
            return name?.ToLower();
        }

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("player");
        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("player {name}", "Display info about the player");
        public bool ShouldShowInHelp => true;
    }
}