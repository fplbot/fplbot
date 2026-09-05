using System.Text.Json.Serialization;


namespace Fpl.Client.Models;

public class EntryHistory
{
    [JsonPropertyName("chips")]
    public ICollection<EntryHistoryChip> Chips { get; set; }

    [JsonPropertyName("past")]
    public ICollection<EntrySeasonHistory> SeasonHistory { get; set; }

    [JsonPropertyName("current")]
    public ICollection<EventEntryHistory> GameweekHistory { get; set; }
}
