using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.WebApi.Slack.Extensions;
using FplBot.Services.WebApi.Slack.Helpers;
using FplBot.Services.WebApi.Slack.Helpers.Formatting;
using MassTransit;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Commands;

public class PlayerCommandHandler(
    ISlackWorkSpacePublisher workSpacePublisher,
    IGlobalSettingsClient globalSettingsClient)
    : IConsumer<ProcessPlayerCommand>
{
    public async Task Consume(ConsumeContext<ProcessPlayerCommand> context)
    {
        var command = context.Message;
        var globalSettings = await globalSettingsClient.GetGlobalSettings();
        var players = globalSettings!.Players;
        var teams = globalSettings.Teams;

        var name = MessageHelper.ExtractArgs(command.Text, "player {args}") ?? "";

        var allPlayers = players.OrderByDescending(player => player.OwnershipPercentage);
        var mostPopularMatchingPlayer = FindMostPopularMatchingPlayer(allPlayers.ToArray(), name);

        if (mostPopularMatchingPlayer == null)
        {
            await workSpacePublisher.PublishToWorkspace(command.TeamId, command.Channel, $"Couldn't find {name}");
            return;
        }

        await workSpacePublisher.PublishToWorkspace(command.TeamId, new ChatPostMessageRequest
        {
            Channel = command.Channel,
            Blocks = SlackFormatter.GetPlayerCard(mostPopularMatchingPlayer, teams)
        });
    }

    private static Player? FindMostPopularMatchingPlayer(Player[] players, string name)
    {
        if (PlayerNickNames.NickNameToRealNameMap.ContainsKey(name))
        {
            name = PlayerNickNames.NickNameToRealNameMap[name];
        }

        var bestMatchInRegularSearch = SearchHelper.Find(
            players,
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
            players,
            name,
            x => (x.SecondName ?? "").Replace("-", " ").Split(" ").Searchable());

        if (IsGoodEnoughMatch(name, bestMatchInSplitSecondNameSearch))
        {
            return bestMatchInSplitSecondNameSearch!.Item;
        }

        var bestMatchInSplitFirstNameSearch = SearchHelper.Find(
            players,
            name,
            x => (x.FirstName ?? "").Replace("-", " ").Split(" ").Searchable());

        if (IsGoodEnoughMatch(name, bestMatchInSplitFirstNameSearch))
        {
            return bestMatchInSplitFirstNameSearch!.Item;
        }

        var bestMatchInAbbreviationSearch = SearchHelper.Find(
            players,
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
