using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class PlayerSeasonHistory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("season_name")]
    public string SeasonName { get; set; }

    [JsonPropertyName("element_code")]
    public int ElementCode { get; set; }

    [JsonPropertyName("start_cost")]
    public int StartCost { get; set; }

    [JsonPropertyName("end_cost")]
    public int EndCost { get; set; }

    [JsonPropertyName("total_points")]
    public int TotalPoints { get; set; }

    [JsonPropertyName("minutes")]
    public int Minutes { get; set; }

    [JsonPropertyName("goals_scored")]
    public int GoalsScored { get; set; }

    [JsonPropertyName("assists")]
    public int Assists { get; set; }

    [JsonPropertyName("clean_sheets")]
    public int CleanSheets { get; set; }

    [JsonPropertyName("goals_conceded")]
    public int GoalsConceded { get; set; }

    [JsonPropertyName("own_goals")]
    public int OwnGoals { get; set; }

    [JsonPropertyName("penalties_saved")]
    public int PenaltiesSaved { get; set; }

    [JsonPropertyName("penalties_missed")]
    public int PenaltiesMissed { get; set; }

    [JsonPropertyName("yellow_cards")]
    public int YellowCards { get; set; }

    [JsonPropertyName("red_cards")]
    public int RedCards { get; set; }

    [JsonPropertyName("saves")]
    public int Saves { get; set; }

    [JsonPropertyName("bonus")]
    public int Bonus { get; set; }

    [JsonPropertyName("bps")]
    public int Bps { get; set; }

    [JsonPropertyName("influence")]
    public double Influence { get; set; }

    [JsonPropertyName("creativity")]
    public double Creativity { get; set; }

    [JsonPropertyName("threat")]
    public double Threat { get; set; }

    [JsonPropertyName("ict_index")]
    public double IctIndex { get; set; }

    [JsonPropertyName("ea_index")]
    public double EaIndex { get; set; }

    [JsonPropertyName("season")]
    public int Season { get; set; }
}