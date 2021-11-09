using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class GlobalSettings
{
    [JsonPropertyName("teams")]
    public ICollection<Team> Teams { get; set; }

    [JsonPropertyName("elements")]
    public ICollection<Player> Players { get; set; }

    [JsonPropertyName("events")]
    public ICollection<Gameweek> Gameweeks { get; set; }

    [JsonPropertyName("element_types")]
    public ICollection<PlayerType> PlayerTypes { get; set; }

    [JsonPropertyName("game_settings")]
    public GameSettings GameSettings { get; set; }

    [JsonPropertyName("phases")]
    public ICollection<Phase> Phases { get; set; }

    [JsonPropertyName("element_stats")]
    public ICollection<ElementStats> StatsOptions { get; set; }

    [JsonPropertyName("total_players")]
    public long TotalPlayers { get; set; }
}