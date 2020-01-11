using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Transfer
    {
        [JsonProperty("element_in")]
        public int ElementIn { get; set; }

        [JsonProperty("element_in_cost")]
        public int ElementInCost { get; set; }

        [JsonProperty("element_out")]
        public int ElementOut { get; set; }

        [JsonProperty("element_out_cost")]
        public int ElementOutCost { get; set; }

        [JsonProperty("entry")]
        public int Entry { get; set; }

        [JsonProperty("event")]
        public int Event { get; set; }

        [JsonProperty("time")]
        public DateTime Time { get; set; }
    }
}
