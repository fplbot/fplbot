using System.Threading.Tasks;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Slack.Helpers;
using NServiceBus;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Models.Events;

namespace FplBot.Slack.Handlers.SlackEvents
{
    public class DebugHandler : IHandleAppMentions
    {
        private readonly IMessageSession _session;

        public DebugHandler(IMessageSession session)
        {
            _session = session;
        }

        public async Task<EventHandledResponse> Handle(EventMetaData eventMetadata, AppMentionEvent slackEvent)
        {
            var debugDetails = MetaService.DebugInfo();
            var debugInfo = $"▪️ v{debugDetails.MajorMinorPatch}\n" +
                            $"▪️ {debugDetails.Informational}\n";
            var releaseNotes = await GitHubReleaseService.GetReleaseNotes(debugDetails.MajorMinorPatch);
            if (!string.IsNullOrEmpty(releaseNotes))
            {
                debugInfo += releaseNotes;
            }
            else if (debugDetails.Sha != "0")
            {
                debugInfo += $"️▪️ <https://github.com/fplbot/fplbot/tree/{debugDetails.Sha}|{debugDetails.Sha?.Substring(0, debugDetails.Sha.Length - 1)}>\n";
            }
            await _session.SendLocal(new PublishToSlack(eventMetadata.Team_Id, slackEvent.Channel, debugInfo));
            return new EventHandledResponse("OK");
        }

        public bool ShouldHandle(AppMentionEvent slackEvent) => slackEvent.Text.Contains("debug");
    }
}
