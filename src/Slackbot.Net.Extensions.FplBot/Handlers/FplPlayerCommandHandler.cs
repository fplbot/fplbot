using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;
using System.Linq;
using System.Threading.Tasks;

namespace Slackbot.Net.Extensions.FplBot.Handlers
{
    internal class FplPlayerCommandHandler : IHandleEvent
    {
        private readonly IPlayerClient _playerClient;
        private readonly ITeamsClient _teamsClient;
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;

        public FplPlayerCommandHandler(
            ISlackWorkSpacePublisher workSpacePublisher,
            IPlayerClient playerClient, 
            ITeamsClient teamsClient)
        {
            _playerClient = playerClient;
            _teamsClient = teamsClient;
            _workSpacePublisher = workSpacePublisher;
        }
        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var message = slackEvent as AppMentionEvent;
            var allPlayersTask = _playerClient.GetAllPlayers();
            var teamsTask = _teamsClient.GetAllTeams();

            var name = ParsePlayerFromInput(message);

            var allPlayers = (await allPlayersTask).OrderByDescending(player => player.OwnershipPercentage);
            var mostPopularMatchingPlayer = FindMostPopularMatchingPlayer(allPlayers.ToArray(), name);

            if (mostPopularMatchingPlayer == null)
            {
                await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, $"Couldn't find {name}");
                return new EventHandledResponse($"Found no matching player for {name}: ");
            }

            var playerName = $"{mostPopularMatchingPlayer.FirstName} {mostPopularMatchingPlayer.SecondName}";
            var teams = await teamsTask;
            
            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, new ChatPostMessageRequest
            {
                Channel = message.Channel,
                Blocks = Formatter.GetPlayerCard(mostPopularMatchingPlayer, teams)
            });
            
            return new EventHandledResponse($"Found matching player for {name}: " + playerName);
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

        private string ParsePlayerFromInput(AppMentionEvent message)
        {
            return new MessageHelper().ExtractArgs(message.Text, "player {args}");
        }

        public bool ShouldHandle(SlackEvent slackEvent) => slackEvent is AppMentionEvent @event && @event.Text.Contains("player");

        public (string,string) GetHelpDescription() => ("player {name}", "Display info about the player");
    }
}