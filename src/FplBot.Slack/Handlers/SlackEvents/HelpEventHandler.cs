using System.Net;
using Fpl.Client.Abstractions;
using FplBot.Formatting;
using FplBot.Slack.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Slack.Handlers.SlackEvents;

public class HelpEventHandler : IShortcutAppMentions
{
    private readonly IEnumerable<IHandleAppMentions> _handlers;
    private readonly ISlackClientBuilder _slackClientService;
    private readonly ISlackTeamRepository _tokenStore;
    private readonly ILogger<HelpEventHandler> _logger;
    private readonly ILeagueClient _leagueClient;

    public HelpEventHandler(IEnumerable<IHandleAppMentions> allHandlers, ISlackClientBuilder slackClientService, ISlackTeamRepository tokenStore, ILogger<HelpEventHandler> logger, ILeagueClient leagueClient)
    {
        _handlers = allHandlers;
        _slackClientService = slackClientService;
        _tokenStore = tokenStore;
        _logger = logger;
        _leagueClient = leagueClient;
    }

    public async Task Handle(EventMetaData eventMetadata, AppMentionEvent @event)
    {
        var team = await _tokenStore.GetTeam(eventMetadata.Team_Id);
        var slackClient = _slackClientService.Build(team.AccessToken);
        var text = $"*HELP:*\n";
        if (team.HasChannelAndLeagueSetup())
        {
            try
            {
                var league = await _leagueClient.GetClassicLeague(team.FplbotLeagueId.Value);
                text += $"Currently following {league.Properties.Name} in {ChannelName()}\n";
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                text += $"Currently following {team.FplbotLeagueId} in {ChannelName()}\n";
            }

            string ChannelName()
            {
                return team.FplBotSlackChannel.StartsWith("#") ? team.FplBotSlackChannel : $"<#{team.FplBotSlackChannel}>";
            }
        }
        else
        {
            text += "Currently not following any leagues\n";
        }

        if(team.Subscriptions.Any())
            text += $"Active subscriptions:\n{Formatter.BulletPoints(team.Subscriptions)}\n";

        await slackClient.ChatPostMessage(@event.Channel, text);
        var handlerHelp = _handlers.Select(handler => handler.GetHelpDescription())
            .Where(desc => !string.IsNullOrEmpty(desc.HandlerTrigger))
            .Aggregate($"\n*Available commands:*", (current, tuple) => current + $"\n• `@fplbot {tuple.HandlerTrigger}` : _{tuple.Description}_");



        await slackClient.ChatPostMessage(new ChatPostMessageRequest { Channel = @event.Channel, Text = handlerHelp, Link_Names = false });
    }

    public bool ShouldShortcut(AppMentionEvent @event)=> @event.Text.Contains("help");
}