using System.Text.RegularExpressions;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class UnknownAppMentionCommandHandler(
    IMessageSession session)
    : INoOpAppMentions
{


    public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        var mentionTextStripped = Regex.Replace(slackEvent.Text, "<@(\\w+)>", "$1");
        await session.Publish(new UnknownAppMentionReceived(eventMetadata.Team_Id, slackEvent.User, mentionTextStripped));
        return new EventHandledResponse("OK");
    }
}
