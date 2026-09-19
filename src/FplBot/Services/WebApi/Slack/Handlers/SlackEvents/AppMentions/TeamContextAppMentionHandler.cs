using System.Diagnostics;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;

public class TeamContextAppMentionHandler(IHandleAppMentions inner, ILogger<TeamContextAppMentionHandler> logger)
    : IHandleAppMentions
{
    public bool ShouldHandle(AppMentionEvent slackEvent) => inner.ShouldHandle(slackEvent);

    public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            [FplBotDiagnostics.TeamIdTag] = eventMetadata.Team_Id,
            [FplBotDiagnostics.ChannelIdTag] = slackEvent.Channel
        });
        Activity.Current?.SetTag(FplBotDiagnostics.TeamIdTag, eventMetadata.Team_Id);
        Activity.Current?.SetTag(FplBotDiagnostics.ChannelIdTag, slackEvent.Channel);
        logger.LogDebug("Handling app mention");

        return await inner.Handle(eventMetadata, slackEvent);
    }
}
