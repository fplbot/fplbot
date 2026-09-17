using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class DebugHandler(IPublishEndpoint publishEndpoint) : IHandleAppMentions
{
    public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        await publishEndpoint.Publish(new ProcessDebugCommand(eventMetadata.Team_Id, slackEvent.Channel));
        return new EventHandledResponse("OK");
    }

    public bool ShouldHandle(AppMentionEvent slackEvent) => slackEvent.Text.Contains("debug");
}
