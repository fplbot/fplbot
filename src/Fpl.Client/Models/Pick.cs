using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class Pick
    {
        [JsonProperty("element")]
        public int PlayerId { get; set; }

        [JsonProperty("element_type")]
        public int ElementType { get; set; }

        [JsonProperty("position")]
        public int TeamPosition { get; set; }

        [JsonProperty("points")]
        public int Points { get; set; }

        [JsonProperty("can_captain")]
        public bool? CanCaptain { get; set; }

        [JsonProperty("is_captain")]
        public bool IsCaptain { get; set; }

        [JsonProperty("is_vice_captain")]
        public bool IsViceCaptain { get; set; }

        [JsonProperty("can_sub")]
        public bool? CanSub { get; set; }

        [JsonProperty("is_sub")]
        public bool IsSub { get; set; }

        [JsonProperty("has_played")]
        public bool HasPlayed { get; set; }

        [JsonProperty("stats")]
        public PickStats Stats { get; set; }

        [JsonProperty("multiplier")]
        public int Multiplier { get; set; }
    }
}
