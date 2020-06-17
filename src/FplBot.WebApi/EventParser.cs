using FplBot.WebApi.EventApi;
using Newtonsoft.Json.Linq;
using Slackbot.Net.Abstractions.Handlers;
using Slackbot.Net.Abstractions.Handlers.Models.Rtm.MessageReceived;

namespace FplBot.WebApi
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
        
        public static SlackEvent ToEventType(JObject eventJson)
        {
            var eventType = GetEventType(eventJson);
            switch (eventType)
            {    
                case EventTypes.AppMention:
                    return eventJson.ToObject<AppMentionEvent>();
                default:
                    return eventJson.ToObject<SlackEvent>();
            }
        }
        
        public static string GetEventType(JObject eventJson)
        {
            if (eventJson != null)
            {
                return eventJson["type"].Value<string>();
            }
            
            return "unknown";
        }
    }
}