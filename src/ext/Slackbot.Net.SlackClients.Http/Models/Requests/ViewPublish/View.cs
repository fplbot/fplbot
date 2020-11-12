using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage.Blocks;

namespace Slackbot.Net.SlackClients.Http.Models.Requests.ViewPublish
{
    public class View
    {
        /// <summary>
        /// Should be either `home` for App Home views, or `modal`
        /// Defaults to : `home`.
        /// </summary>
        public string Type { get; set; } = PublishViewConstants.Home;
        
        /// <summary>
        /// The title that appears in the top-left of the modal. Must be a plain_text text element with a max length of 24 characters.
        ///
        /// Only applicable for Modals
        /// </summary>
        public Text Title { get; set; }
        
        /// <summary>
        /// 	An array of blocks that defines the content of the view. Max of 100 blocks.
        /// 
        /// Used for Modals, Home tabs
        /// </summary>
        public IBlock[] Blocks { get; set; }
        
        /// <summary>
        /// 	An optional string that will be sent to your app in view_submission and block_actions events. Max length of 3000 characters.
        /// Used for Modals Home tabs
        /// </summary>
        public string Private_Metadata { get; set; }

        /// <summary>
        /// A custom identifier that must be unique for all views on a per-team basis.
        /// Used for Modals Home tabs
        /// </summary>
        public string External_Id { get; set; }
    }
}