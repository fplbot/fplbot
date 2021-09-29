using System;
using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class NewLeagueEntry
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("entry_name")]
        public string EntryName { get; set; }

        [JsonPropertyName("player_first_name")]
        public string PlayerFirstName { get; set; }

        [JsonPropertyName("player_last_name")]
        public string PlayerLastName { get; set; }

        [JsonPropertyName("joined_time")]
        public DateTime JoinedAt { get; set; }

        [JsonPropertyName("entry")]
        public int Entry { get; set; }

        [JsonPropertyName("league")]
        public int League { get; set; }
    }
}
