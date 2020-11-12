namespace Slackbot.Net.Endpoints.Models.Events
{
    public class MemberJoinedChannelEvent : SlackEvent
    {
        public string User { get; set; }
        public string Channel { get; set; }
        public string Channel_Type { get; set; }
        public string Inviter { get; set; }
        public string Ts { get; set; }
        public string Event_Ts { get; set; }
    }
}