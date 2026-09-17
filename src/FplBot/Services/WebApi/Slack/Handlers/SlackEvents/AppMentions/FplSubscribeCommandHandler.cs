using FplBot.ApplicationServices.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

internal class FplSubscribeCommandHandler(IPublishEndpoint publishEndpoint) : HandleAppMentionBase
{
    public override string[] Commands => ["subscribe", "unsubscribe"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
    {
        await publishEndpoint.Publish(new ProcessSubscribeCommand(eventMetadata.Team_Id, appMentioned.Channel, appMentioned.Text));
        return new EventHandledResponse("OK");
    }

    public override (string, string) GetHelpDescription() => (SlackCommandCatalog.Subscribe.Trigger, SlackCommandCatalog.Subscribe.Description);
}
