using fplbot.consoleapp.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace fplbot.consoleapp.Clients
{
    public class FplClient : IFplClient
    {
        private readonly HttpClient _httpClient;

        public FplClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ScoreBoard> GetScoreBoard(string leagueId)
        {
            var response = await _httpClient.GetAsync($"https://fantasy.premierleague.com/api/leagues-classic/{leagueId}/standings/");
            var result = JsonConvert.DeserializeObject<ScoreBoard>(await response.Content.ReadAsStringAsync());
            return result;
        }
    }

    public interface IFplClient
    {
        Task<ScoreBoard> GetScoreBoard(string leagueId);
    }
}
