

using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class EventEntryHistory
{
    [JsonPropertyName("event")]
    public int Event { get; set; }

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("total_points")]
    public int TotalPoints { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("rank_sort")]
    public int? RankSort { get; set; }

    [JsonPropertyName("overall_rank")]
    public int? OverallRank { get; set; }

    [JsonPropertyName("bank")]
    public int Bank { get; set; }

    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("event_transfers")]
    public int EventTransfers { get; set; }

    [JsonPropertyName("event_transfers_cost")]
    public int EventTransfersCost { get; set; }

    [JsonPropertyName("points_on_bench")]
    public int PointsOnBench { get; set; }
}
