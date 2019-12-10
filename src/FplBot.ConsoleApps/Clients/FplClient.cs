using FplBot.ConsoleApps.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
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

        public async Task<PlayerStats> GetPlayerData(string playerId)
        {
            return await Get<PlayerStats>($"https://fantasy.premierleague.com/entry/{playerId}/history");
        }

        public async Task<string> GetStandings(string leagueId)
        {

            var scoreBoardTask = Get<ScoreBoard>($"https://fantasy.premierleague.com/api/leagues-classic/{leagueId}/standings/");
            var bootstrapTask = Get<Bootstrap>("https://fantasy.premierleague.com/api/bootstrap-static/");

            var scoreBoard = await scoreBoardTask;
            var bootStrap = await bootstrapTask;

            return Formatter.GetStandings(scoreBoard, bootStrap);
        }

        public async Task<string> GetAllFplDataForPlayer(string name)
        {
            var bootstrapTask = Get<Bootstrap>("https://fantasy.premierleague.com/api/bootstrap-static/");

            var bootStrap = await bootstrapTask;

            name = name.ToLower();

            var matchingPlayers = bootStrap.Elements
                .Where((p) => p.FirstName.ToLower().Contains(name) || p.LastName.ToLower().Contains(name));

            return matchingPlayers.Any() ? string.Join("\n", matchingPlayers) : "";
        }

        private async Task<T> Get<T>(string url)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("User-Agent", "Lol");
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

    public interface IFplClient
    {
        Task<string> GetStandings(string leagueId);
        Task<string> GetAllFplDataForPlayer(string name);
    }
}
