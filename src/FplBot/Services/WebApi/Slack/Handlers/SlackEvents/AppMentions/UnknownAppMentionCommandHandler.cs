using System.Text.RegularExpressions;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class UnknownAppMentionCommandHandler(IPublishEndpoint publishEndpoint) : INoOpAppMentions
{
    public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        var mentionTextStripped = Regex.Replace(slackEvent.Text, "<@(\\w+)>", "$1");
        await publishEndpoint.Publish(new ProcessUnknownAppMention(eventMetadata.Team_Id, slackEvent.Channel, slackEvent.User, slackEvent.Ts, mentionTextStripped));
        return new EventHandledResponse("OK");
    }
}
