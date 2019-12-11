using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Clients;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class FplClient : IFplClient
    {
        private readonly HttpClient _httpClient;

        public FplClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PlayerStats> GetPlayerData(string playerId)
        {
            return await Get<PlayerStats>($"/entry/{playerId}/history");
        }

        public async Task<ScoreBoard> GetScoreBoard(string leagueId)
        {
            return await Get<ScoreBoard>($"/api/leagues-classic/{leagueId}/standings/");
        }
        
        public async Task<Bootstrap> GetBootstrap()
        {
            return await Get<Bootstrap>("/api/bootstrap-static/");
        }

        private async Task<T> Get<T>(string url)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url, UriKind.Relative)
            };
           
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(body) || !body.StartsWith("{"))
            {
                throw new FplApiException($"FPL er litt uenig. Status {response.StatusCode} - {body}");    
            }
            
            var result = JsonConvert.DeserializeObject<T>(body);
            return result;
        }
    }
}
