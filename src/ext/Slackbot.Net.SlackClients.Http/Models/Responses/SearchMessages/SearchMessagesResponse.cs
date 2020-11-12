namespace Slackbot.Net.SlackClients.Http.Models.Responses.SearchMessages
{
    public class SearchMessagesResponse : Response
    {
        public SearchResponseMessagesContainer Messages;
    }

    public class SearchResponseMessagesContainer
    {
        public ContextMessage[] Matches;
    }

    public class ContextMessage : Message
    {
        public Message Previous_2;
        public Message Previous;
        public Message Next;
        public Message Next_2;
    }
    
    public class Error
    {
        public int Code;
        public string Msg;
    }
    
    public class SlackSocketMessage
    {
        public bool Ok = true;
        public int Id;
        public int Reply_To;
        public string Type;
        public string SubType;
        public Error Error;
    }
    
    public class Reaction
    {
        public string Name { get; set; }
        public string Channel { get; set; }
        public string Timestamp { get; set; }
    }

    public class Message : SlackSocketMessage
    {
        public Channel Channel;
        public string Ts; //epoch
        public string User;
        public string Username;
        public string Text;
        public bool Is_Starred;
        public string Permalink;
        public Reaction[] Reactions;
    }

    public class Channel
    {
        public string Name;
        public string Id;
    }
}