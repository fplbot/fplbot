using System.Net;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Fpl.Client;

public class LeagueClient(HttpClient client, ICacheProvider cache) : ILeagueClient
{
    public async Task<ClassicLeague?> GetClassicLeague(int leagueId, int page = 1, bool tolerate404 = false)
    {
        try
        {
            return await cache.GetCachedOrFetch<ClassicLeague>(
                $"/api/leagues-classic/{leagueId}/standings/?page_standings={page}",
                client.GetStringAsync,
                TimeSpan.FromMinutes(5)); //max-age=300
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
