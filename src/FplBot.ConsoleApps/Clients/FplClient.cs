using FplBot.ConsoleApps.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FplBot.ConsoleApps.Clients
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
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://fantasy.premierleague.com/api/leagues-classic/{leagueId}/standings/"),
            };
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("User-Agent", "Lol");
            var response = await _httpClient.SendAsync(request);
            var test = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ScoreBoard>(test);
            return result;
        }
    }

    public interface IFplClient
    {
        Task<ScoreBoard> GetScoreBoard(string leagueId);
    }
}
