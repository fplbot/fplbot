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
using Slackbot.Net.Extensions.FplBot.Extensions;

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

            var allPlayers = (await allPlayersTask).OrderByDescending(player => player.OwnershipPercentage);
            var mostPopularMatchingPlayer = FindMostPopularMatchingPlayer(allPlayers.ToArray(), name);

            if (mostPopularMatchingPlayer == null)
            {
                await _slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = message.ChatHub.Id,
                    Text = $"Couldn't find {name}"
                });
                return new HandleResponse($"Found no matching player for {name}: ");
            }

            var playerName = $"{mostPopularMatchingPlayer.FirstName} {mostPopularMatchingPlayer.SecondName}";
            var teams = await teamsTask;
            await _slackClient.ChatPostMessage(new ChatPostMessageRequest
            {
                Channel = message.ChatHub.Id,
                Blocks = Formatter.GetPlayerCard(mostPopularMatchingPlayer, teams)
            });
            
            return new HandleResponse($"Found matching player for {name}: " + playerName);
        }

        private static Player FindMostPopularMatchingPlayer(Player[] players, string name)
        {
            var mostPopularMatchingPlayer = SearchHelper.Find(
                players, 
                name, 
                x => $"{x.FirstName} {x.SecondName}",
                x => x.SecondName, 
                x => x.FirstName,
                x => x.WebName);

            if (mostPopularMatchingPlayer == null || mostPopularMatchingPlayer.LevenshteinDistance > 1 && name.Length < 4)
            {
                var matchingAbbreviatedPlayer = SearchHelper.Find(
                    players, 
                    name,
                    x => $"{x.FirstName} {x.SecondName}".Abbreviated());

                if (matchingAbbreviatedPlayer.LevenshteinDistance == 0)
                {
                    mostPopularMatchingPlayer = matchingAbbreviatedPlayer;
                }
            }

            return mostPopularMatchingPlayer?.Item;
        }

        private string ParsePlayerFromInput(SlackMessage message)
        {
            return _messageHelper.ExtractArgs(message.Text, "player {args}");
        }

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("player");
        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("player {name}", "Display info about the player");
        public bool ShouldShowInHelp => true;
    }
}