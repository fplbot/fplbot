using Slackbot.Net.Models.BlockKit;

namespace Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage
{
    public class ChatPostMessageRequest
    {
        public string Channel { get; set; }
        public string Text { get; set; }
        public string Parse { get; set; }

        public bool Link_Names { get; set; } = true;
        public string thread_ts { get; set; }
        public string unfurl_links { get; set; }
        public string unfurl_media { get; set; }
        public string as_user { get; set; }
        
        public Attachment[] attachments { get; set; }
        
        public IBlock[] Blocks { get; set; }
    }
    
    public class Attachment
    {
        public string callback_id{ get; set; }
        public string fallback { get; set; }
        public string color { get; set; }
        public string pretext { get; set; }
        public string author_name { get; set; }
        public string author_link { get; set; }
        public string author_icon { get; set; }
        public string title { get; set; }
        public string title_link { get; set; }
        public string text { get; set; }
        public Field[] fields { get; set; }
        public string image_url { get; set; }
        public string thumb_url { get; set; }
        public string[] mrkdwn_in { get; set; }
        public AttachmentAction[] actions { get; set; }
        public string footer { get; set; }
        public string footer_icon { get; set; }
    }
    
    public class Field
    {
        public string title { get; set; }
        public string value { get; set; }
        public bool @short { get; set; }
    }
    
    public class AttachmentAction
    {
        public string type = "button";
        public string style { get; set; }
        public string value { get; set; }
        public ActionConfirm confirm { get; set; }

        public AttachmentAction(string name, string text)
        {
            this.name = name;
            this.text = text;
        }

        public string name { get; }

        public string text { get; }
    }
    
    public class ActionConfirm
    {
        public string title { get; set; }
        public string text { get; set; }
        public string ok_text { get; set; }
        public string dismiss_text { get; set; }
    }
}