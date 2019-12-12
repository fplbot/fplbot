using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class EntryPicks
    {
        [JsonProperty("entry_history")]
        public EventEntryHistory EventEntryHistory { get; set; }

        [JsonProperty("automatic_subs")]
        public ICollection<AutomaticSub> AutomaticSubs { get; set; }

        [JsonProperty("picks")]
        public ICollection<Pick> Picks { get; set; }

        [JsonProperty("active_chip")]
        public string ActiveChip { get; set; }
    }
}
