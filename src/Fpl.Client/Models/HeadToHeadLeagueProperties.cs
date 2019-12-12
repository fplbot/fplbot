using System;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class HeadToHeadLeagueProperties
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("has_started")]
        public bool HasStarted { get; set; }

        [JsonProperty("can_delete")]
        public bool CanDelete { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("created")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("closed")]
        public bool Closed { get; set; }

        [JsonProperty("forum_disabled")]
        public bool ForumDisabled { get; set; }

        [JsonProperty("make_code_public")]
        public bool MakeCodePublic { get; set; }

        [JsonProperty("rank")]
        public int? Rank { get; set; }

        [JsonProperty("size")]
        public int? Size { get; set; }

        [JsonProperty("league_type")]
        public string LeagueType { get; set; }

        [JsonProperty("_scoring")]
        public string Scoring { get; set; }

        [JsonProperty("ko_rounds")]
        public int ReprocessStandings { get; set; }

        [JsonProperty("admin_entry")]
        public int AdminEntry { get; set; }

        [JsonProperty("start_event")]
        public int StartEvent { get; set; }
    }
}
