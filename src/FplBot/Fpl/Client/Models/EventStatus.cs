using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public record EventStatusResponse
{
    [JsonPropertyName("status")]
    public ICollection<EventStatus> Status { get; set; } = new List<EventStatus>();

    [JsonPropertyName("leagues")]
    public string Leagues { get; set; }
}

public record EventStatus
{
    [JsonPropertyName("bonus_added")]
    public bool BonusAdded { get; set; }

    /// <summary>
    /// YYYY-MM-dd
    /// </summary>
    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("event")]
    public int Event { get; set; }

    [JsonPropertyName("points")]
    public string PointsStatus { get; set; }
}

public class EventStatusConstants
{
    public class LeaguesStatus
    {
        public const string Nothing = "";
        public const string Updating = "Updating";
        public const string Updated = "Updated";
    }

    public class PointStatus
    {
        public const string Nothing = "";
        public const string Live = "l";
        public const string Ready = "r";
    }
}
