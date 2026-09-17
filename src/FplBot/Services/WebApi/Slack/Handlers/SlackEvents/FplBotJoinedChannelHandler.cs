using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents;

public class FplBotJoinedChannelHandler(IPublishEndpoint publishEndpoint) : IHandleMemberJoinedChannel
{
    public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, MemberJoinedChannelEvent joinedEvent)
    {
        await publishEndpoint.Publish(new ProcessBotJoinedChannel(eventMetadata.Team_Id, joinedEvent.Channel, joinedEvent.User));
        return new EventHandledResponse("OK");
    }
}
