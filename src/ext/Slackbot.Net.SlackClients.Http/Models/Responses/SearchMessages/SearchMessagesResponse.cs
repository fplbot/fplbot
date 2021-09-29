namespace Slackbot.Net.SlackClients.Http.Models.Responses.SearchMessages
{
    public class SearchMessagesResponse : Response
    {
        public SearchResponseMessagesContainer Messages { get; set; }
    }

    public class SearchResponseMessagesContainer
    {
        public ContextMessage[] Matches { get; set; }
    }

    public class ContextMessage : Message
    {
        public Message Previous_2{ get; set; }
        public Message Previous { get; set; }
        public Message Next { get; set; }
        public Message Next_2 { get; set; }
    }
    
    public class Error
    {
        public int Code { get; set; }
        public string Msg { get; set; }
    }
    
    public class SlackSocketMessage
    {
        public bool Ok{ get; set; } = true;
        public int Id { get; set; }
        public int Reply_To { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public Error Error { get; set; }
    }
    
    public class Reaction
    {
        public string Name { get; set; }
        public string Channel { get; set; }
        public string Timestamp { get; set; }
    }

    public class Message : SlackSocketMessage
    {
        public Channel Channel { get; set; }
        public string Ts{ get; set; } //epoch
        public string User{ get; set; }
        public string Username{ get; set; }
        public string Text{ get; set; }
        public bool Is_Starred{ get; set; }
        public string Permalink{ get; set; }
        public Reaction[] Reactions{ get; set; }
    }

    public class Channel
    {
        public string Name{ get; set; }
        public string Id{ get; set; }
    }
}