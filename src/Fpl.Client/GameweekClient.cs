using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class GameweekClient : IGameweekClient
    {
        private readonly HttpClient _client;

        public GameweekClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ICollection<Gameweek>> GetGameweeks()
        {
           const string url = "/api/bootstrap-static/";

            var json = await _client.GetStringAsync(url);

            var data = JsonConvert.DeserializeObject<GlobalSettings>(json);

            return data.Events;
        }
    }
}
