using System.Net;
using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Formatting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class HelpEventHandler(
    IEnumerable<IHandleAppMentions> allHandlers,
    ISlackClientBuilder slackClientService,
    ISlackTeamRepository tokenStore,
    ILeagueClient leagueClient)
    : IShortcutAppMentions
{
    public async Task Handle(EventMetaData eventMetadata, AppMentionEvent @event)
    {
        var installation = await tokenStore.GetInstallation(eventMetadata.Team_Id);
        var channel = installation.GetChannel(@event.Channel);
        var slackClient = slackClientService.Build(installation.Token);
        var text = "*HELP:*\n";
        if (channel?.FollowedLeagueId is not null)
        {
            var leagueId = channel!.FollowedLeagueId!.Value;
            try
            {
                var league = await leagueClient.GetClassicLeague((int)leagueId);
                text += $"Currently following {league?.Properties?.Name} in {ChannelName()}\n";
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                text += $"Currently following {leagueId} in {ChannelName()}\n";
            }

            string ChannelName()
            {
                return channel.ChannelId.StartsWith("#") ? channel.ChannelId : $"<#{channel.ChannelId}>";
            }
        }
        else
        {
            text += "Currently not following any leagues\n";
        }

        if(channel is not null && channel.Events.Current.Any())
            text += $"Active subscriptions:\n{Formatter.BulletPoints(channel.Events.Current)}\n";

        await slackClient.ChatPostMessage(@event.Channel, text);
        var handlerHelp = allHandlers.Select(handler => handler.GetHelpDescription())
            .Where(desc => !string.IsNullOrEmpty(desc.HandlerTrigger))
            .Aggregate("\n*Available commands:*", (current, tuple) => current + $"\n• `@fplbot {tuple.HandlerTrigger}` : _{tuple.Description}_");



        await slackClient.ChatPostMessage(new ChatPostMessageRequest { Channel = @event.Channel, Text = handlerHelp, Link_Names = false });
    }

    public bool ShouldShortcut(AppMentionEvent @event)=> @event.Text.Contains("help");
}
