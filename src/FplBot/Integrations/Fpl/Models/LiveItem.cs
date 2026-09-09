using System.Text.Json.Serialization;

namespace Fpl.Client.Models;

public class LiveResponse
{
    public ICollection<LiveItem> Elements { get; set; } = [];
}

public class LiveItem
{
    public int Id { get; set; }
    public LiveItemStat? Stats { get; set; }
    public ICollection<LiveItemExplain> Explain { get; set; } = [];
}

public class LiveItemStat
{
    [JsonPropertyName("total_points")]
    public int TotalPoints { get; set; }
}

// Per-fixture points breakdown. A player has one entry per fixture played in the gameweek,
// so a double gameweek player has two.
public class LiveItemExplain
{
    public int Fixture { get; set; }
    public ICollection<LiveItemExplainStat> Stats { get; set; } = [];
}

public class LiveItemExplainStat
{
    public string? Identifier { get; set; }
    public int Points { get; set; }
    public int Value { get; set; }
}
