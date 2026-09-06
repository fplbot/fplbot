using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class Team
{
    [JsonPropertyName("code")]
    public long Code { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("short_name")]
    public string? ShortName { get; set; }
}
