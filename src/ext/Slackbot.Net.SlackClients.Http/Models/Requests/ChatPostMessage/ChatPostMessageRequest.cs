using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage.Blocks;

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
        public string callback_id;
        public string fallback;
        public string color;
        public string pretext;
        public string author_name;
        public string author_link;
        public string author_icon;
        public string title;
        public string title_link;
        public string text;
        public Field[] fields;
        public string image_url;
        public string thumb_url;
        public string[] mrkdwn_in;
        public AttachmentAction[] actions;
        public string footer;
        public string footer_icon;
    }
    
    public class Field
    {
        public string title;
        public string value;
        public bool @short;
    }
    
    public class AttachmentAction
    {
        public string type = "button";
        public string style;
        public string value;
        public ActionConfirm confirm;

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
        public string title;
        public string text;
        public string ok_text;
        public string dismiss_text;
    }
}