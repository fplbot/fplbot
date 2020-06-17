namespace Slackbot.Net.Endpoints.Models
{
    public class AppMentionEvent : SlackEvent
    {
        public string Text { get; set; }
        public string Channel { get; set; }
        public string Ts { get; set; }
        public string Event_Ts { get; set; }
    }
}