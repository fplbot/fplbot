using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class Pick
    {
        [JsonPropertyName("element")]
        public int PlayerId { get; set; }

        [JsonPropertyName("element_type")]
        public int ElementType { get; set; }

        [JsonPropertyName("position")]
        public int TeamPosition { get; set; }

        [JsonPropertyName("points")]
        public int Points { get; set; }

        [JsonPropertyName("can_captain")]
        public bool? CanCaptain { get; set; }

        [JsonPropertyName("is_captain")]
        public bool IsCaptain { get; set; }

        [JsonPropertyName("is_vice_captain")]
        public bool IsViceCaptain { get; set; }

        [JsonPropertyName("can_sub")]
        public bool? CanSub { get; set; }

        [JsonPropertyName("is_sub")]
        public bool IsSub { get; set; }

        [JsonPropertyName("has_played")]
        public bool HasPlayed { get; set; }

        [JsonPropertyName("stats")]
        public PickStats Stats { get; set; }

        [JsonPropertyName("multiplier")]
        public int Multiplier { get; set; }
    }
}
