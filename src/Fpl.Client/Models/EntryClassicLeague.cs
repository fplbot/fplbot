using System;
using Newtonsoft.Json;

namespace Fpl.Client.Models
{
    public class EntryClassicLeague
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("entry_rank")]
        public int? EntryRank { get; set; }

        [JsonProperty("entry_last_rank")]
        public int? EntryLastRank { get; set; }

        [JsonProperty("entry_movement")]
        public string EntryMovement { get; set; }

        [JsonProperty("entry_change")]
        public bool? EntryChange { get; set; }

        [JsonProperty("entry_can_leave")]
        public bool? EntryCanLeave { get; set; }

        [JsonProperty("entry_can_admin")]
        public bool? EntryCanAdmin { get; set; }

        [JsonProperty("entry_can_invite")]
        public bool? EntryCanInvite { get; set; }

        [JsonProperty("entry_can_forum")]
        public bool? EntryCanForum { get; set; }

        [JsonProperty("entry_code")]
        public string EntryCode { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("is_cup")]
        public bool IsCup { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("closed")]
        public bool? Closed { get; set; }

        [JsonProperty("forum_disabled")]
        public bool? ForumDisabled { get; set; }

        [JsonProperty("make_code_public")]
        public bool? MakeCodePublic { get; set; }

        [JsonProperty("rank")]
        public int? Rank { get; set; }

        [JsonProperty("size")]
        public int? Size { get; set; }

        [JsonProperty("league_type")]
        public string LeagueType { get; set; }

        [JsonProperty("_scoring")]
        public string Scoring { get; set; }

        [JsonProperty("reprocess_standings")]
        public bool ReprocessStandings { get; set; }

        [JsonProperty("admin_entry")]
        public int? AdminEntry { get; set; }

        [JsonProperty("start_event")]
        public int StartEvent { get; set; }
    }
}
