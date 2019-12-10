using FplBot.ConsoleApps.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            return await Get<PlayerStats>($"/entry/{playerId}/history");
        }

        public async Task<string> GetStandings(string leagueId)
        {

            var scoreBoardTask = Get<ScoreBoard>($"/api/leagues-classic/{leagueId}/standings/");
            var bootstrapTask = Get<Bootstrap>("/api/bootstrap-static/");

            var scoreBoard = await scoreBoardTask;
            var bootStrap = await bootstrapTask;

            return Formatter.GetStandings(scoreBoard, bootStrap);
        }

        public async Task<string> GetAllFplDataForPlayer(string name)
        {
            var bootstrapTask = Get<Bootstrap>("/api/bootstrap-static/");

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
                RequestUri = new Uri(url, UriKind.Relative)
            };

            var sessionCookie = await GetSessionCookie();
            request.Headers.Add("Cookie", sessionCookie);
           
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(body) || !body.StartsWith("{"))
            {
                throw new FplApiException($"FPL er litt uenig. Status {response.StatusCode} - {body}");    
            }
            
            var result = JsonConvert.DeserializeObject<T>(body);
            return result;
        }

        private async Task<string> GetSessionCookie()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://users.premierleague.com/accounts/login/", UriKind.Absolute),
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["login"] = "ls@blank.no",
                    ["password"] = "***",
                    ["app"] = "plfpl-web",
                    ["redirect_uri"] = "https://fantasy.premierleague.com/"
                }),
                Headers = {
                    { "Origin", "https://fantasy.premierleague.com" },
                    { "Referer", "https://fantasy.premierleague.com/" }
                }
            };

            var response = await _httpClient.SendAsync(request);
            return string.Join(", ", response.Headers.GetValues("Set-Cookie").Select(x => x.Split(';').First()));
        }
    }
}
