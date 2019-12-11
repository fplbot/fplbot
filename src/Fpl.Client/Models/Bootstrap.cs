using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Bootstrap
    {
        public IEnumerable<Event> Events { get; set; }

        public IEnumerable<Element> Elements { get; set; }
    }

    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonProperty("is_current")]
        public bool IsCurrent { get; set; }
    }

    public class Element
    {
        [JsonProperty("first_name")]
        public string FirstName { get; set; }
        [JsonProperty("second_name")]
        public string LastName { get; set; }

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }
}
