using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;


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
            return await _client.GetFromJsonAsync<EntryHistory>($"/api/entry/{teamId}/history/", JsonConvert.JsonSerializerOptions);
        }
    }
}
