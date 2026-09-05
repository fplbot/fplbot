using System.Text.Json.Serialization;


namespace Fpl.Client.Models;

public class EntryPicks
{
    [JsonPropertyName("entry_history")]
    public EventEntryHistory EventEntryHistory { get; set; }

    [JsonPropertyName("automatic_subs")]
    public ICollection<AutomaticSub> AutomaticSubs { get; set; }

    [JsonPropertyName("picks")]
    public ICollection<Pick> Picks { get; set; }

    [JsonPropertyName("active_chip")]
    public string ActiveChip { get; set; }
}
