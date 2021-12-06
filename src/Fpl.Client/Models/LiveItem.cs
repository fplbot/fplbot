using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class LiveResponse
{
    public ICollection<LiveItem> Elements { get; set; }
}

public class LiveItem
{
    public int Id { get; set; }
    public LiveItemStat Stats { get; set; }
}

public class LiveItemStat
{
    [JsonPropertyName("total_points")]
    public int TotalPoints { get; set; }
}
