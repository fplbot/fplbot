using FplBot.WebApi.Controllers;

namespace FplBot.WebApi.EventApi
{
    public class BotMentionedEvent : SlackEvent
    {
        public string Text { get; set; }
        public string Channel { get; set; }
        public string Ts { get; set; }
        public string Event_Ts { get; set; }
    }
}