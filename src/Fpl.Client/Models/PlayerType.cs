using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class PlayerType
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("plural_name")]
        public string PluralName { get; set; }

        [JsonProperty("plural_name_short")]
        public string PluralNameShort { get; set; }

        [JsonProperty("singular_name")]
        public string SingularName { get; set; }

        [JsonProperty("singular_name_short")]
        public string SingularNameShort { get; set; }
    }
}
