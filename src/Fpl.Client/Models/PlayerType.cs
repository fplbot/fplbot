using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class PlayerType
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("plural_name")]
    public string PluralName { get; set; }

    [JsonPropertyName("plural_name_short")]
    public string PluralNameShort { get; set; }

    [JsonPropertyName("singular_name")]
    public string SingularName { get; set; }

    [JsonPropertyName("singular_name_short")]
    public string SingularNameShort { get; set; }
}