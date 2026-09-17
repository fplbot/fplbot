using FplBot.ApplicationServices.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

internal class FplCaptainCommandHandler(IPublishEndpoint publishEndpoint) : HandleAppMentionBase
{
    public override string[] Commands => ["captains"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent incomingMessage)
    {
        await publishEndpoint.Publish(new ProcessCaptainsCommand(eventMetadata.Team_Id, incomingMessage.Channel, incomingMessage.Text));
        return new EventHandledResponse("OK");
    }

    public override (string, string) GetHelpDescription() => (SlackCommandCatalog.Captains.Trigger, SlackCommandCatalog.Captains.Description);
}
