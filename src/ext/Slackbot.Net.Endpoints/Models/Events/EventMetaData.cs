namespace Slackbot.Net.Endpoints.Models.Events
{
    public class EventMetaData
    {
        public string Token { get; set; }
        public string Team_Id { get; set; }
        public string Type { get; set; }
        public string[] AuthedUsers { get; set; }
        public string Event_Id { get; set; }
        public long Event_Time { get; set; }
    }
}