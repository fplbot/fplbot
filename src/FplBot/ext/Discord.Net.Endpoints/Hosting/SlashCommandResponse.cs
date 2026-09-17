using System.Text.Json.Serialization;

namespace Discord.Net.Endpoints.Hosting;

public abstract class SlashCommandResponse
{
    [JsonIgnore]
    public int Type { get; protected init; }
}
