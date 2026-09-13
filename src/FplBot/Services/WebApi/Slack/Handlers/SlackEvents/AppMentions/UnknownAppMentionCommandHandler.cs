using System.Text.RegularExpressions;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class UnknownAppMentionCommandHandler(
    IPublishEndpoint publishEndpoint,
    ISlackTeamRepository teamRepository,
    ISlackClientBuilder builder)
    : INoOpAppMentions
{
    public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        var mentionTextStripped = Regex.Replace(slackEvent.Text, "<@(\\w+)>", "$1");
        await publishEndpoint.Publish(new UnknownAppMentionReceived(eventMetadata.Team_Id, slackEvent.User, mentionTextStripped));
        var installation = await teamRepository.GetInstallation(eventMetadata.Team_Id);
        var slackClient = builder.Build(installation.Token);
        await slackClient.ChatPostMessage(
            new ChatPostMessageRequest
            {
                Channel = slackEvent.Channel,
                thread_ts = slackEvent.Ts,
                Text = "🤷‍♀️ Ok, that clearly did not work. Maybe try the `help` command to see my available commands?"
            });
        return new EventHandledResponse("OK");
    }
}
