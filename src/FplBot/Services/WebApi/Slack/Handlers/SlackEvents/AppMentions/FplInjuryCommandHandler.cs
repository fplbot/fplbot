using FplBot.ApplicationServices.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

internal class FplInjuryCommandHandler(IPublishEndpoint publishEndpoint) : HandleAppMentionBase
{
    public override string[] Commands => ["injuries"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent message)
    {
        await publishEndpoint.Publish(new ProcessInjuriesCommand(eventMetadata.Team_Id, message.Channel));
        return new EventHandledResponse("OK");
    }

    public override (string, string) GetHelpDescription() => (SlackCommandCatalog.Injuries.Trigger, SlackCommandCatalog.Injuries.Description);
}
