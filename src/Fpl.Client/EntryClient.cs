using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class EntryClient : IEntryClient
    {
        private readonly HttpClient _client;

        public EntryClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<BasicEntry> Get(int teamId)
        {
            var json = await _client.GetStringAsync($"/api/entry/{teamId}/");

            return JsonConvert.DeserializeObject<BasicEntry>(json);
        }

        public async Task<EntryPicks> GetPicks(int teamId, int gameweek)
        {
            var json = await _client.GetStringAsync($"/api/entry/{teamId}/event/{gameweek}/picks/");

            return JsonConvert.DeserializeObject<EntryPicks>(json);
        }
    }
}
