using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fpl.EventPublishers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers;

internal class PulseLiveClient(HttpClient client, ILogger<PulseLiveClient> logger) : IPulseLiveClient
{
    public async Task<MatchDetails> GetMatchDetails(int pulseId)
    {
        try
        {
            var res = await client.GetAsync($"/api/v3/matches/{pulseId}/lineups");
            res.EnsureSuccessStatusCode();
            var content = await res.Content.ReadAsStringAsync();
            if (content.First() is '{')
            {
                return JsonSerializer.Deserialize<MatchDetails>(content,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
            }
            throw new Exception("Response was not JSON:\n" + content);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return null;
        }
    }
}

public class MatchDetails
{
    [JsonPropertyName("home_team")]
    public TeamLineup HomeTeam { get; set; }

    [JsonPropertyName("away_team")]
    public TeamLineup AwayTeam { get; set; }

    public bool HasLineUps() => HomeTeam?.HasLineups() == true && AwayTeam?.HasLineups() == true;
    public bool HasTeams() => HomeTeam != null && AwayTeam != null;
}

public class TeamLineup
{
    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("players")]
    public IEnumerable<PulsePlayer> Players { get; set; } = new List<PulsePlayer>();

    [JsonPropertyName("formation")]
    public PulseFormation Formation { get; set; }

    public bool HasLineups() =>
        Players != null && Players.Any() &&
        Formation?.Lineup != null && Formation.Lineup.Any();
}

public class PulsePlayer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string LastName { get; set; }

    [JsonPropertyName("knownName")]
    public string KnownName { get; set; }

    [JsonPropertyName("isCaptain")]
    public bool IsCaptain { get; set; }

    [JsonPropertyName("position")]
    public string Position { get; set; }

    public string DisplayName => KnownName ?? LastName ?? $"{FirstName} {LastName}".Trim();

    public string MatchPosition => Position switch
    {
        "Goalkeeper" => "G",
        "Defender" => "D",
        "Midfielder" => "M",
        "Forward" => "F",
        _ => Position
    };
}

public class PulseFormation
{
    [JsonPropertyName("formation")]
    public string Label { get; set; }

    [JsonPropertyName("lineup")]
    public IEnumerable<IEnumerable<int>> Lineup { get; set; }
}
