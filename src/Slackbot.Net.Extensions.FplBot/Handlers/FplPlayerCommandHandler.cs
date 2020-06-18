using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using System;
using System.Linq;
using System.Threading.Tasks;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplPlayerCommandHandler : IHandleMessages, IHandleEvent
    {
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly ISlackClientService _slackClientService;

        public FplPlayerCommandHandler(
            ISlackClientService slackClient, 
            IPlayerClient playerClient, 
            ITeamsClient teamsClient)
        {
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _slackClientService = slackClient;
        }
        public async Task<HandleResponse> Handle(SlackMessage message)
        {
            var allPlayersTask = _playerClient.GetAllPlayers();
            var teamsTask = _teamsClient.GetAllTeams();

            var name = ParsePlayerFromInput(message);

            var allPlayers = (await allPlayersTask).OrderByDescending(player => player.OwnershipPercentage);
            var mostPopularMatchingPlayer = FindMostPopularMatchingPlayer(allPlayers.ToArray(), name);
            var slackClient = await _slackClientService.CreateClient(message.Team.Id);

            if (mostPopularMatchingPlayer == null)
            {
                await slackClient.ChatPostMessage(new ChatPostMessageRequest
                {
                    Channel = message.ChatHub.Id,
                    Text = $"Couldn't find {name}"
                });
                return new HandleResponse($"Found no matching player for {name}: ");
            }

            var playerName = $"{mostPopularMatchingPlayer.FirstName} {mostPopularMatchingPlayer.SecondName}";
            var teams = await teamsTask;
            await slackClient.ChatPostMessage(new ChatPostMessageRequest
            {
                Channel = message.ChatHub.Id,
                Blocks = Formatter.GetPlayerCard(mostPopularMatchingPlayer, teams)
            });
            
            return new HandleResponse($"Found matching player for {name}: " + playerName);
        }
        
        public async Task Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var rtmMessage = EventParser.ToBackCompatRtmMessage(eventMetadata, slackEvent);
            await Handle(rtmMessage);
        }

        private static Player FindMostPopularMatchingPlayer(Player[] players, string name)
        {
            if (PlayerNickNames.NickNameToRealNameMap.ContainsKey(name))
            {
                name = PlayerNickNames.NickNameToRealNameMap[name];
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
            return new MessageHelper(message.Bot).ExtractArgs(message.Text, "player {args}");
        }

        public bool ShouldHandle(SlackMessage message) => message.MentionsBot && message.Text.Contains("player");
        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("player");

        public Tuple<string, string> GetHelpDescription() => new Tuple<string, string>("player {name}", "Display info about the player");
        public bool ShouldShowInHelp => true;
    }
}