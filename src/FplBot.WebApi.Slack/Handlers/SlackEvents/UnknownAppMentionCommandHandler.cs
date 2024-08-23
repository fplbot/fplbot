using System.Text.RegularExpressions;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.WebApi.Slack.Handlers.SlackEvents;

public class UnknownAppMentionCommandHandler(
    IMessageSession session,
    ISlackTeamRepository teamRepository,
    ISlackClientBuilder builder)
    : INoOpAppMentions
{
    public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        var mentionTextStripped = Regex.Replace(slackEvent.Text, "<@(\\w+)>", "$1");
        await session.Publish(new UnknownAppMentionReceived(eventMetadata.Team_Id, slackEvent.User, mentionTextStripped));
        var team = await teamRepository.GetTeam(eventMetadata.Team_Id);
        var slackClient = builder.Build(team.AccessToken);
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
