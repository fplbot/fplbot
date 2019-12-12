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
           var url = $"/api/entry/{teamId}/";

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<BasicEntry>(json);
        }

        public async Task<EntryPicks> GetPicks(int teamId, int gameweek)
        {
           var url = $"/api/entry/{teamId}/event/{gameweek}/picks/";

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<EntryPicks>(json);
        }
    }
}
