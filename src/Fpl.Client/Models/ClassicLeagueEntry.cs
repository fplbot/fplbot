using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class ClassicLeagueEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("entry_name")]
    public string EntryName { get; set; }

    [JsonPropertyName("event_total")]
    public int EventTotal { get; set; }

    [JsonPropertyName("player_name")]
    public string PlayerName { get; set; }

    [JsonPropertyName("movement")]
    public string Movement { get; set; }

    [JsonPropertyName("own_entry")]
    public bool OwnEntry { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("last_rank")]
    public int LastRank { get; set; }

    [JsonPropertyName("rank_sort")]
    public int RankSort { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("entry")]
    public int Entry { get; set; }

    [JsonPropertyName("league")]
    public int League { get; set; }

    [JsonPropertyName("start_event")]
    public int StartEvent { get; set; }

    [JsonPropertyName("stop_event")]
    public int StopEvent { get; set; }
}
