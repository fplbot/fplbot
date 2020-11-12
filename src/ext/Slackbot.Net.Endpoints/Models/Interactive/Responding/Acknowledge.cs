namespace Slackbot.Net.Endpoints.Models.Interactive.Responding
{
    public class Acknowledge
    {
        public string Text
        {
            get;
            set;
        }

        public bool Is_Ephemeral
        {
            get;
            set;
        }

        public bool Delete_Original
        {
            get;
            set;
        }
    }
}