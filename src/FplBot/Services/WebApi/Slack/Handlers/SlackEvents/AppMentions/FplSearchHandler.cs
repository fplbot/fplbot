using FplBot.ApplicationServices.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class FplSearchHandler(IPublishEndpoint publishEndpoint) : HandleAppMentionBase
{
    public override string[] Commands => ["search"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
    {
        await publishEndpoint.Publish(new ProcessSearchCommand(eventMetadata.Team_Id, message.Channel, message.User, message.Text));
        return new EventHandledResponse("OK");
    }

    public override (string, string) GetHelpDescription() => (SlackCommandCatalog.Search.Trigger, SlackCommandCatalog.Search.Description);
}
