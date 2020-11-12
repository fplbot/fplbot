using System.Collections.Generic;
using Slackbot.Net.Endpoints.Models.Interactive.ViewSubmissions;

namespace Slackbot.Net.Endpoints.Models.Interactive.BlockActions
{
    public class BlockActionInteraction : Interaction
    {
        public Team Team { get; set; }
        public User User { get; set; }
        
        public IEnumerable<ActionsBlock> Actions { get; set; }
        
    }
}