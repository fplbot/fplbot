using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class EventEntry
    {
        [JsonProperty("leagues")]
        public EntryLeagues Leagues { get; set; }

        [JsonProperty("entry_history")]
        public EventEntryHistory EventEntryHistory { get; set; }

        [JsonProperty("ce")]
        public string Ce { get; set; }

        [JsonProperty("automatic_subs")]
        public ICollection<AutomaticSub> AutomaticSubs { get; set; }

        [JsonProperty("fixtures")]
        public ICollection<Fixture> Fixture { get; set; }

        [JsonProperty("can_change_captain")]
        public bool CanChangeCaptain { get; set; }

        [JsonProperty("entry")]
        public Entry Entry { get; set; }

        [JsonProperty("picks")]
        public ICollection<Pick> Picks { get; set; }

        [JsonProperty("own_entry")]
        public bool OwnEntry { get; set; }

        [JsonProperty("state")]
        public EventEntryState State {get; set; }

        [JsonProperty("points")]
        public int Points { get; set; }

        [JsonProperty("active_chip")]
        public string ActiveChip { get; set; }
    }
}
