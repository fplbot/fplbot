using Fpl.Client.Models;
using FplBot.Services.WebApi.Slack.Extensions;
using FplBot.Services.WebApi.Slack.Helpers;
using FplBot.Services.WebApi.Slack.Helpers.Formatting;

namespace FplBot.Formatting.Helpers;

public class PlayerSearch : IPlayerSearch
{
    public Player? FindMostPopularMatchingPlayer(ICollection<Player> players, string name)
    {
        var orderedPlayers = players.OrderByDescending(player => player.OwnershipPercentage).ToArray();

        if (PlayerNickNames.NickNameToRealNameMap.ContainsKey(name))
        {
            name = PlayerNickNames.NickNameToRealNameMap[name];
        }

        var bestMatchInRegularSearch = SearchHelper.Find(
            orderedPlayers,
            name,
            x => $"{x.FirstName} {x.SecondName}".Searchable(),
            x => (x.SecondName ?? "").Searchable(),
            x => (x.FirstName ?? "").Searchable(),
            x => (x.WebName ?? "").Searchable());

        if (IsGoodEnoughMatch(name, bestMatchInRegularSearch))
        {
            return bestMatchInRegularSearch!.Item;
        }

        var bestMatchInSplitSecondNameSearch = SearchHelper.Find(
            orderedPlayers,
            name,
            x => (x.SecondName ?? "").Replace("-", " ").Split(" ").Searchable());

        if (IsGoodEnoughMatch(name, bestMatchInSplitSecondNameSearch))
        {
            return bestMatchInSplitSecondNameSearch!.Item;
        }

        var bestMatchInSplitFirstNameSearch = SearchHelper.Find(
            orderedPlayers,
            name,
            x => (x.FirstName ?? "").Replace("-", " ").Split(" ").Searchable());

        if (IsGoodEnoughMatch(name, bestMatchInSplitFirstNameSearch))
        {
            return bestMatchInSplitFirstNameSearch!.Item;
        }

        var bestMatchInAbbreviationSearch = SearchHelper.Find(
            orderedPlayers,
            name,
            x => $"{x.FirstName} {x.SecondName}".Abbreviated().Searchable());

        if (IsPerfectMatch(bestMatchInAbbreviationSearch))
        {
            return bestMatchInAbbreviationSearch!.Item;
        }

        return bestMatchInRegularSearch?.Item;
    }

    private static bool IsGoodEnoughMatch(string name, SearchResult<Player>? mostPopularMatchingPlayer)
    {
        return mostPopularMatchingPlayer != null && mostPopularMatchingPlayer.LevenshteinDistance < 2 && name.Length > 3;
    }

    private static bool IsPerfectMatch(SearchResult<Player>? mostPopularMatchingPlayer)
    {
        return mostPopularMatchingPlayer != null && mostPopularMatchingPlayer.LevenshteinDistance == 0;
    }
}
