using Fpl.Client.Abstractions;
using Fpl.Client.Models;

using System.Net;
using System.Net.Http.Json;

namespace Fpl.Client;

public class LeagueClient : ILeagueClient
{
    private readonly HttpClient _client;

    public LeagueClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<ClassicLeague> GetClassicLeague(int leagueId, int page = 1, bool tolerate404 = false)
    {
        try
        {
            return await _client.GetFromJsonAsync<ClassicLeague>($"/api/leagues-classic/{leagueId}/standings/?page_standings={page}", JsonConvert.JsonSerializerOptions);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
        {
            return null;
        }
    }

    public async Task<HeadToHeadLeague> GetHeadToHeadLeague(int leagueId)
    {
        return await _client.GetFromJsonAsync<HeadToHeadLeague>($"/api/leagues-h2h/{leagueId}/standings/", JsonConvert.JsonSerializerOptions);
    }
}
