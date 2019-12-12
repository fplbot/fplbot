using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class EntryLeagues
    {
        [JsonProperty("h2h")]
        public IList<EntryHeadToHeadLeague> HeadToHeadLeagues { get; set; }

        [JsonProperty("classic")]
        public IList<EntryClassicLeague> ClassicLeagues { get; set; } 
    }
}
