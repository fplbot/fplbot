using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class EntryLeagues
{
    [JsonPropertyName("h2h")]
    public IList<EntryHeadToHeadLeague> HeadToHeadLeagues { get; set; }

    [JsonPropertyName("classic")]
    public IList<EntryClassicLeague> ClassicLeagues { get; set; }
}
