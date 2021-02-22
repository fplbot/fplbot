using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class LiveResponse
    {
        public ICollection<LiveItem> Elements { get; set; }
    }

    public class LiveItem
    {
        public int Id { get; set; }
        public LiveItemStat Stats { get; set; }
    }

    public class LiveItemStat
    {
        [JsonProperty("total_points")]
        public int TotalPoints { get; set; }
    }
}