using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace FplBot.ConsoleApps.Models
{
    public class Bootstrap
    {
        public IEnumerable<Event> Events { get; set; }
    }

    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonProperty("is_current")]
        public bool IsCurrent { get; set; }
    }
}
