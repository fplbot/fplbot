using System.Diagnostics;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class HelpEventHandler(ILogger<HelpEventHandler> logger, IPublishEndpoint publishEndpoint) : IShortcutAppMentions
{
    public async Task Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            [FplBotDiagnostics.TeamIdTag] = eventMetadata.Team_Id,
            [FplBotDiagnostics.ChannelIdTag] = slackEvent.Channel
        });
        Activity.Current?.SetTag(FplBotDiagnostics.TeamIdTag, eventMetadata.Team_Id);
        Activity.Current?.SetTag(FplBotDiagnostics.ChannelIdTag, slackEvent.Channel);
        logger.LogDebug("Handling help request");
        await publishEndpoint.Publish(new ProcessHelpCommand(eventMetadata.Team_Id, slackEvent.Channel));
    }

    public bool ShouldShortcut(AppMentionEvent @event) => @event.Text.Contains("help");
}
