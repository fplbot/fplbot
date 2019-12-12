using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class EntryHistoryClient : IEntryHistoryClient
    {
        private readonly HttpClient _client;

        public EntryHistoryClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<EntryHistory> GetHistory(int teamId)
        {
            var json = await _client.GetStringAsync($"/api/entry/{teamId}/history/");

            return JsonConvert.DeserializeObject<EntryHistory>(json);
        }
    }
}
