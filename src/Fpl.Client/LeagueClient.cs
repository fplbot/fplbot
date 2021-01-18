using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class LeagueClient : ILeagueClient
    {
        private readonly HttpClient _client;

        public LeagueClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ClassicLeague> GetClassicLeague(int leagueId, int page = 1)
        {
            var json = await _client.GetStringAsync($"/api/leagues-classic/{leagueId}/standings/?page_standings={page}");
            
            return JsonConvert.DeserializeObject<ClassicLeague>(json);
        }

        public async Task<HeadToHeadLeague> GetHeadToHeadLeague(int leagueId)
        {
            var json = await _client.GetStringAsync($"/api/leagues-h2h/{leagueId}/standings/");

            return JsonConvert.DeserializeObject<HeadToHeadLeague>(json);
        }
    }
}
