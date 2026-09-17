using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class HelpEventHandler(IPublishEndpoint publishEndpoint) : IShortcutAppMentions
{
    public async Task Handle(EventMetaData eventMetadata, AppMentionEvent @event)
    {
        await publishEndpoint.Publish(new ProcessHelpCommand(eventMetadata.Team_Id, @event.Channel));
    }

    public bool ShouldShortcut(AppMentionEvent @event) => @event.Text.Contains("help");
}
