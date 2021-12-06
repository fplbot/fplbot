using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class Team
{
    [JsonPropertyName("code")]
    public long Code { get; set; }

    [JsonPropertyName("draw")]
    public long Draw { get; set; }

    [JsonPropertyName("form")]
    public object Form { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("link_url")]
    public string LinkUrl { get; set; }

    [JsonPropertyName("loss")]
    public long Loss { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("played")]
    public long Played { get; set; }

    [JsonPropertyName("points")]
    public long Points { get; set; }

    [JsonPropertyName("position")]
    public long Position { get; set; }

    [JsonPropertyName("short_name")]
    public string ShortName { get; set; }

    [JsonPropertyName("strength")]
    public long Strength { get; set; }

    [JsonPropertyName("strength_attack_away")]
    public long StrengthAttackAway { get; set; }

    [JsonPropertyName("strength_attack_home")]
    public long StrengthAttackHome { get; set; }

    [JsonPropertyName("strength_defence_away")]
    public long StrengthDefenceAway { get; set; }

    [JsonPropertyName("strength_defence_home")]
    public long StrengthDefenceHome { get; set; }

    [JsonPropertyName("strength_overall_away")]
    public long StrengthOverallAway { get; set; }

    [JsonPropertyName("strength_overall_home")]
    public long StrengthOverallHome { get; set; }

    [JsonPropertyName("team_division")]
    public long? TeamDivision { get; set; }

    [JsonPropertyName("unavailable")]
    public bool Unavailable { get; set; }

    [JsonPropertyName("win")]
    public long Win { get; set; }

}
