using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class LiveClient : ILiveClient
    {
        private readonly HttpClient _client;

        public LiveClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ICollection<LiveItem>> GetLiveItems(int gameweek)
        {
            var json = await _client.GetStringAsync($"/api/event/{gameweek}/live/");

            var data = JsonConvert.DeserializeObject<LiveResponse>(json);

            return data.Elements;
        }
    }
}
