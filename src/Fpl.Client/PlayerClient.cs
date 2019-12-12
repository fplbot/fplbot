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
            var json = await _client.GetStringAsync("/api/bootstrap-static/");

            var data = JsonConvert.DeserializeObject<GlobalSettings>(json);

            return data.Players;
        }

        public async Task<PlayerSummary> GetPlayer(int playerId)
        {
            var json = await _client.GetStringAsync($"/api/element-summary/{playerId}/");
            
            return JsonConvert.DeserializeObject<PlayerSummary>(json);
        }
    }
}
