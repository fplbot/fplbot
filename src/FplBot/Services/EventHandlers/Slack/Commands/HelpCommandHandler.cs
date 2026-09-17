using System.Net;
using Fpl.Client.Abstractions;
using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack.Commands;

public class HelpCommandHandler(
    ISlackWorkSpacePublisher publisher,
    ISlackTeamRepository teamRepo,
    ILeagueClient leagueClient)
    : IConsumer<ProcessHelpCommand>
{
    public async Task Consume(ConsumeContext<ProcessHelpCommand> context)
    {
        var command = context.Message;
        var installation = await teamRepo.GetInstallation(command.TeamId);
        var channel = installation.GetChannel(command.Channel);
        var text = "*HELP:*\n";
        if (channel?.FollowedLeagueId is not null)
        {
            var leagueId = channel.FollowedLeagueId.Value;
            try
            {
                var league = await leagueClient.GetClassicLeague((int)leagueId);
                text += $"Currently following {league?.Properties?.Name} in {ChannelName(channel.ChannelId)}\n";
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                text += $"Currently following {leagueId} in {ChannelName(channel.ChannelId)}\n";
            }
        }
        else
        {
            text += "Currently not following any leagues\n";
        }

        if (channel is not null && channel.Events.Current.Any())
            text += $"Active subscriptions:\n{Formatter.BulletPoints(channel.Events.Current)}\n";

        await publisher.PublishToWorkspace(command.TeamId, command.Channel, text);

        var handlerHelp = SlackCommandCatalog.All
            .Aggregate("\n*Available commands:*", (current, c) => current + $"\n• `@fplbot {c.Trigger}` : _{c.Description}_");

        await publisher.PublishToWorkspace(command.TeamId, new ChatPostMessageRequest
        {
            Channel = command.Channel, Text = handlerHelp, Link_Names = false
        });
    }

    // Back-compat as we currently have a mix of display names (#name) and channel_ids (C12351)
    private static string ChannelName(string channelId) =>
        channelId.StartsWith("#") ? channelId : $"<#{channelId}>";
}
