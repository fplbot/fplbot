using Fpl.Client.Abstractions;
using Fpl.Client.Models;

using System.Net;

namespace Fpl.Client;

public class LeagueClient(HttpClient client) : ILeagueClient
{
    public async Task<ClassicLeague?> GetClassicLeague(int leagueId, int page = 1, bool tolerate404 = false)
    {
        try
        {
            return await client.GetFromJsonAsync<ClassicLeague>($"/api/leagues-classic/{leagueId}/standings/?page_standings={page}", JsonConvert.JsonSerializerOptions);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
        {
            return null;
        }
    }

    public async Task<HeadToHeadLeague?> GetHeadToHeadLeague(int leagueId)
    {
        return await client.GetFromJsonAsync<HeadToHeadLeague>($"/api/leagues-h2h/{leagueId}/standings/", JsonConvert.JsonSerializerOptions);
    }
}
