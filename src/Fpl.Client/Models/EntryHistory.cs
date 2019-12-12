using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class EntryHistory
    {
        [JsonProperty("chips")]
        public ICollection<EntryHistoryChip> Chips { get; set; }

        [JsonProperty("past")]
        public ICollection<EntrySeasonHistory> SeasonHistory { get; set; }

        [JsonProperty("current")]
        public ICollection<EventEntryHistory> GameweekHistory { get; set; }
    }
}
