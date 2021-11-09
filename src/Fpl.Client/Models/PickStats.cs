using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class PickStats
{
    [JsonPropertyName("yellow_cards")]
    public int YellowCards { get; set; }

    [JsonPropertyName("own_goals")]
    public int OwnGoals { get; set; }

    [JsonPropertyName("creativity")]
    public double Creativity { get; set; }

    [JsonPropertyName("goals_conceded")]
    public int GoalsConceded { get; set; }

    [JsonPropertyName("bonus")]
    public int Bonus { get; set; }

    [JsonPropertyName("red_cards")]
    public int RedCards { get; set; }

    [JsonPropertyName("saves")]
    public int Saves { get; set; }

    [JsonPropertyName("influence")]
    public double Influence { get; set; }

    [JsonPropertyName("bps")]
    public int Bps { get; set; }

    [JsonPropertyName("clean_sheets")]
    public int CleanSheets { get; set; }

    [JsonPropertyName("assists")]
    public int Assists { get; set; }

    [JsonPropertyName("ict_index")]
    public double IctIndex { get; set; }

    [JsonPropertyName("goals_scored")]
    public int GoalsScored { get; set; }

    [JsonPropertyName("threat")]
    public double Threat { get; set; }

    [JsonPropertyName("penalties_missed")]
    public int PenaltiesMissed { get; set; }

    [JsonPropertyName("total_points")]
    public int TotalPoints { get; set; }

    [JsonPropertyName("penalties_saved")]
    public int PenaltiesSaved { get; set; }

    [JsonPropertyName("in_dreamteam")]
    public bool InDreamteam { get; set; }

    [JsonPropertyName("minutes")]
    public int Minutes { get; set; }
}