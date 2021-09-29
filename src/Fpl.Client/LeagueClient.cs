using Fpl.Client.Abstractions;
using Fpl.Client.Models;

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Client
{
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
                var json = await _client.GetStringAsync($"/api/leagues-classic/{leagueId}/standings/?page_standings={page}");
                return JsonConvert.DeserializeObject<ClassicLeague>(json);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
            {
                return null;
            }
        }

        public async Task<HeadToHeadLeague> GetHeadToHeadLeague(int leagueId)
        {
            var json = await _client.GetStringAsync($"/api/leagues-h2h/{leagueId}/standings/");

            return JsonConvert.DeserializeObject<HeadToHeadLeague>(json);
        }
    }
}
