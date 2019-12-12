using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class PlayerClient : IPlayerClient
    {
        private readonly HttpClient _client;

        public PlayerClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ICollection<Player>> GetAllPlayers()
        {
           const string url = "/api/bootstrap-static/";

            var json = await _client.GetStringAsync(url);

            var data = JsonConvert.DeserializeObject<GlobalSettings>(json);

            return data.Players;
        }

        public async Task<PlayerSummary> GetPlayer(int playerId)
        {
           var url = PlayerSummaryUrlFor(playerId);

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<PlayerSummary>(json);
        }

        private static string PlayerSummaryUrlFor(int playerId)
        {
            return $"http://fantasy.premierleague.com/api/element-summary/{playerId}/";
        }
    }
}
