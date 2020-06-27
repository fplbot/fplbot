using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;
using Slackbot.Net.Endpoints.Models;

namespace Slackbot.Net.Extensions.FplBot
{
    // Hack: map to RTM api models (Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived.SlackMessage)
    // until migration to Event API models in handlers
    public static class EventParser
    {
        public static SlackMessage ToBackCompatRtmMessage(EventMetaData eventMetadata, SlackEvent slackEvent)
        {
            var eventTyped = slackEvent;
            var (text, channelId) = GetTextAndChannel(eventTyped);
            
            return new SlackMessage
            {
                Bot = new BotDetails()
                {
                    Id = "we-dont-get-this-value-from-the-event-api-and-it-changes-between-installs-in-different-workspaces",
                    Name = "we-dont-get-this-value-from-the-event-api"
                },
                Text = text,
                MentionsBot = eventTyped is AppMentionEvent,
                ChatHub = new ChatHub
                {
                    Id = channelId,
                    Name = "we-dont-get-this-value-from-the-event-api"
                },
                Team = new TeamDetails
                {
                    Id = eventMetadata.Team_Id,
                    Name = "we-dont-get-this-value-from-the-event-api"
                }
            };
        }
        
        private static (string text, string channelId) GetTextAndChannel(SlackEvent eventTyped)
        {
            switch (eventTyped)
            {
                case AppMentionEvent e:
                    return (e.Text,e.Channel);
                default:
                    return ("unsupported-eventtype", "unsupported-eventtype");
            }
        }
    }
}