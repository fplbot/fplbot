using Fpl.Client.Abstractions;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.WebApi.Slack.Helpers;
using FplBot.Services.WebApi.Slack.Helpers.Formatting;
using MassTransit;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Commands;

public class PlayerCommandHandler(
    ISlackWorkSpacePublisher workSpacePublisher,
    IGlobalSettingsClient globalSettingsClient,
    IPlayerImageClient playerImageClient,
    IPlayerSearch playerSearch)
    : IConsumer<ProcessPlayerCommand>
{
    public async Task Consume(ConsumeContext<ProcessPlayerCommand> context)
    {
        var command = context.Message;
        var globalSettings = await globalSettingsClient.GetGlobalSettings();
        var players = globalSettings!.Players;
        var teams = globalSettings.Teams;

        var name = MessageHelper.ExtractArgs(command.Text, "player {args}") ?? "";

        var mostPopularMatchingPlayer = playerSearch.FindMostPopularMatchingPlayer(players, name);

        if (mostPopularMatchingPlayer == null)
        {
            await workSpacePublisher.PublishToWorkspace(command.TeamId, command.ChannelId, $"Couldn't find {name}");
            return;
        }

        var imageUrl = await playerImageClient.GetPlayerImageUrl(mostPopularMatchingPlayer.Code);

        await workSpacePublisher.PublishToWorkspace(command.TeamId,
            new ChatPostMessageRequest { Channel = command.ChannelId, Blocks = SlackFormatter.GetPlayerCard(mostPopularMatchingPlayer, teams, imageUrl) });
    }
}
