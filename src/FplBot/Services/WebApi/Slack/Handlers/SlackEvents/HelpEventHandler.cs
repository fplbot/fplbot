using System.Net;
using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Formatting;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class HelpEventHandler(
    IEnumerable<IHandleAppMentions> allHandlers,
    ISlackClientBuilder slackClientService,
    ISlackTeamRepository tokenStore,
    ILogger<HelpEventHandler> logger,
    ILeagueClient leagueClient)
    : IShortcutAppMentions
{
    private readonly ILogger<HelpEventHandler> _logger = logger;

    public async Task Handle(EventMetaData eventMetadata, AppMentionEvent @event)
    {
        var team = await tokenStore.GetTeam(eventMetadata.Team_Id);
        var slackClient = slackClientService.Build(team.AccessToken);
        var text = $"*HELP:*\n";
        if (team.HasChannelAndLeagueSetup())
        {
            try
            {
                var league = await leagueClient.GetClassicLeague(team.FplbotLeagueId!.Value);
                text += $"Currently following {league?.Properties?.Name} in {ChannelName()}\n";
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                text += $"Currently following {team.FplbotLeagueId} in {ChannelName()}\n";
            }

            string ChannelName()
            {
                return team.FplBotSlackChannel?.StartsWith("#") == true ? team.FplBotSlackChannel : $"<#{team.FplBotSlackChannel}>";
            }
        }
        else
        {
            text += "Currently not following any leagues\n";
        }

        if(team.Subscriptions.Any())
            text += $"Active subscriptions:\n{Formatter.BulletPoints(team.Subscriptions)}\n";

        await slackClient.ChatPostMessage(@event.Channel, text);
        var handlerHelp = allHandlers.Select(handler => handler.GetHelpDescription())
            .Where(desc => !string.IsNullOrEmpty(desc.HandlerTrigger))
            .Aggregate($"\n*Available commands:*", (current, tuple) => current + $"\n• `@fplbot {tuple.HandlerTrigger}` : _{tuple.Description}_");



        await slackClient.ChatPostMessage(new ChatPostMessageRequest { Channel = @event.Channel, Text = handlerHelp, Link_Names = false });
    }

    public bool ShouldShortcut(AppMentionEvent @event)=> @event.Text.Contains("help");
}
