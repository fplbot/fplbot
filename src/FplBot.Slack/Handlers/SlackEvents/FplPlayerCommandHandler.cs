using System.Linq;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Formatting;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Extensions;
using FplBot.Slack.Helpers;
using FplBot.Slack.Helpers.Formatting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Slack.Handlers.SlackEvents
{
    internal class FplPlayerCommandHandler : HandleAppMentionBase
    {
        private readonly ISlackWorkSpacePublisher _workSpacePublisher;
        private IGlobalSettingsClient _globalSettingsClient;

        public FplPlayerCommandHandler(
            ISlackWorkSpacePublisher workSpacePublisher,
            IGlobalSettingsClient globalSettingsClient)
        {
            _workSpacePublisher = workSpacePublisher;
            _globalSettingsClient = globalSettingsClient;
        }

        public override string[] Commands => new[] { "player" };

        public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
        {
            var globalSettings = await _globalSettingsClient.GetGlobalSettings();
            var players = globalSettings.Players;
            var teams = globalSettings.Teams;

            var name = ParseArguments(message);

            var allPlayers = players.OrderByDescending(player => player.OwnershipPercentage);
            var mostPopularMatchingPlayer = FindMostPopularMatchingPlayer(allPlayers.ToArray(), name);

            if (mostPopularMatchingPlayer == null)
            {
                await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, message.Channel, $"Couldn't find {name}");
                return new EventHandledResponse($"Found no matching player for {name}: ");
            }

            var playerName = $"{mostPopularMatchingPlayer.FirstName} {mostPopularMatchingPlayer.SecondName}";

            await _workSpacePublisher.PublishToWorkspace(eventMetadata.Team_Id, new ChatPostMessageRequest
            {
                Channel = message.Channel,
                Blocks = SlackFormatter.GetPlayerCard(mostPopularMatchingPlayer, teams)
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

        public override (string,string) GetHelpDescription() => ($"{CommandsFormatted} {{name}}", "Display info about the player");
    }
}
