using FplBot.ApplicationServices.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

internal class FplStandingsCommandHandler(IPublishEndpoint publishEndpoint) : HandleAppMentionBase
{
    public override string[] Commands => ["standings"];

    public override async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent appMentioned)
    {
        await publishEndpoint.Publish(new ProcessStandingsCommand(eventMetadata.Team_Id, appMentioned.Channel));
        return new EventHandledResponse("OK");
    }

    public override (string, string) GetHelpDescription() => (SlackCommandCatalog.Standings.Trigger, SlackCommandCatalog.Standings.Description);
}
