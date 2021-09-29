using System.Collections.Generic;

namespace Slackbot.Net.SlackClients.Http.Models.Responses.ConversationsList
{
    public class ConversationsListResponse : Response
    {
        public IEnumerable<Conversation> Channels { get; set; }
        
        public ResponseMetadata Response_Metadata { get; set; }
    }

    public class ResponseMetadata
    {
        public string Next_Cursor { get; set; }
    }
    
    public class Conversation
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool Is_Channel { get; set; }
        public bool Is_Group { get; set; }
        public bool Is_Im { get; set; }
        public bool Is_General { get; set; }
        public bool Is_Archived { get; set; }
    }
}