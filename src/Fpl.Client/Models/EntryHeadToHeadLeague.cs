using System;
using System.Text.Json.Serialization;

namespace Fpl.Client.Models
{
    public class EntryHeadToHeadLeague
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("entry_rank")]
        public int? EntryRank { get; set; }

        [JsonPropertyName("entry_last_rank")]
        public int? EntryLastRank { get; set; }

        [JsonPropertyName("entry_movement")]
        public string EntryMovement { get; set; }

        [JsonPropertyName("entry_change")]
        public bool? EntryChange { get; set; }

        [JsonPropertyName("entry_can_leave")]
        public bool? EntryCanLeave { get; set; }

        [JsonPropertyName("entry_can_admin")]
        public bool? EntryCanAdmin { get; set; }

        [JsonPropertyName("entry_can_invite")]
        public bool? EntryCanInvite { get; set; }

        [JsonPropertyName("entry_can_forum")]
        public bool? EntryCanForum { get; set; }

        [JsonPropertyName("entry_code")]
        public string EntryCode { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("is_cup")]
        public bool IsCup { get; set; }

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("closed")]
        public bool? Closed { get; set; }

        [JsonPropertyName("forum_disabled")]
        public bool? ForumDisabled { get; set; }

        [JsonPropertyName("make_code_public")]
        public bool? MakeCodePublic { get; set; }

        [JsonPropertyName("rank")]
        public int? Rank { get; set; }

        [JsonPropertyName("size")]
        public int? Size { get; set; }

        [JsonPropertyName("league_type")]
        public string LeagueType { get; set; }

        [JsonPropertyName("_scoring")]
        public string Scoring { get; set; }

        [JsonPropertyName("ko_rounds")]
        public int KnockoutRounds { get; set; }

        [JsonPropertyName("admin_entry")]
        public int? AdminEntry { get; set; }

        [JsonPropertyName("start_event")]
        public int StartEvent { get; set; }
    }
}
