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
using Microsoft.Extensions.Options;
using Slackbot.Net.Extensions.FplBot.Extensions;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplPlayerCommandHandler : IHandleMessages
    {
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly IMessageHelper _messageHelper;
        private readonly ISlackClient _slackClient;
        private readonly FplbotOptions _fplbotOptions;

        public FplPlayerCommandHandler(
            ISlackClient slackClient, 
            IPlayerClient playerClient, 
            ITeamsClient teamsClient, 
            IMessageHelper messageHelper,
            IOptions<FplbotOptions> options)
        {
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _messageHelper = messageHelper;
            _slackClient = slackClient;
            _fplbotOptions = options.Value;
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

        private Player FindMostPopularMatchingPlayer(Player[] players, string name)
        {
            var matchingNickName = _fplbotOptions.NickNames.FirstOrDefault(x => x.NickName == name);
            if (matchingNickName != null)
            {
                name = matchingNickName.RealName;
            }

            var bestMatchInRegularSearch = SearchHelper.Find(
                players, 
                name, 
                x => $"{x.FirstName} {x.SecondName}".Searchable(),
                x => x.SecondName.Searchable(), 
                x => x.FirstName.Searchable(),
                x => x.WebName.Searchable());

            if (IsGoodEnoughMatch(name, bestMatchInRegularSearch))
            {
                return bestMatchInRegularSearch.Item;
            }

            var bestMatchInSplitSecondNameSearch = SearchHelper.Find(
                players,
                name,
                x => x.SecondName.Replace("-", " ").Split(" ").Searchable());

            if (IsGoodEnoughMatch(name, bestMatchInSplitSecondNameSearch))
            {
                return bestMatchInSplitSecondNameSearch.Item;
            }

            var bestMatchInSplitFirstNameSearch = SearchHelper.Find(
                players,
                name,
                x => x.FirstName.Replace("-", " ").Split(" ").Searchable());

            if (IsGoodEnoughMatch(name, bestMatchInSplitFirstNameSearch))
            {
                return bestMatchInSplitFirstNameSearch.Item;
            }

            var bestMatchInAbbreviationSearch = SearchHelper.Find(
                players, 
                name,
                x => $"{x.FirstName} {x.SecondName}".Abbreviated().Searchable());

            if (IsPerfectMatch(bestMatchInAbbreviationSearch))
            {
                return bestMatchInAbbreviationSearch.Item;
            }

            return bestMatchInRegularSearch?.Item;
        }

        private static bool IsGoodEnoughMatch(string name, SearchResult<Player> mostPopularMatchingPlayer)
        {
            return mostPopularMatchingPlayer != null && mostPopularMatchingPlayer.LevenshteinDistance < 2 && name.Length > 3;
        }

        private static bool IsPerfectMatch(SearchResult<Player> mostPopularMatchingPlayer)
        {
            return mostPopularMatchingPlayer != null && mostPopularMatchingPlayer.LevenshteinDistance == 0;
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