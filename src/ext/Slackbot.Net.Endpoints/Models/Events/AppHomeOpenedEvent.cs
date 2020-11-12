namespace Slackbot.Net.Endpoints.Models.Events
{
    public class AppHomeOpenedEvent : SlackEvent
    {
        public string Tab { get; set; }
        public string User { get; set; }
        public string Channel { get; set; }
        public string Event_Ts { get; set; }
    }
}