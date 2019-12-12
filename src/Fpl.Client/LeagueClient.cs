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
           var url = ClassicLeagueUrlFor(leagueId, page);

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<ClassicLeague>(json);
        }

        public async Task<HeadToHeadLeague> GetHeadToHeadLeague(int leagueId, int page = 1)
        {
           var url = HeadToHeadLeagueUrlFor(leagueId, page);

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<HeadToHeadLeague>(json);
        }

        private static string ClassicLeagueUrlFor(int leagueId, int? page)
        {
            var baseUrl = $"http://fantasy.premierleague.com/api/leagues-classic/{leagueId}/standings/";

            var suffix = $"?page_new_entries={page ?? 1}&page_standings={page ?? 1}";

            return $"{baseUrl}{suffix}";
        }

        private static string HeadToHeadLeagueUrlFor(int leagueId, int? page)
        {
            var baseUrl = $"http://fantasy.premierleague.com/api/leagues-h2h/{leagueId}/standings/";

            var suffix = $"?page_new_entries={page ?? 1}&page_standings={page ?? 1}";

            return $"{baseUrl}{suffix}";
        }
    }
}
