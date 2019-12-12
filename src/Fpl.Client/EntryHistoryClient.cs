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
           var url = HistoryUrlFor(teamId);

            var json = await _client.GetStringAsync(url);

            return JsonConvert.DeserializeObject<EntryHistory>(json);
        }

        private static string HistoryUrlFor(int teamId)
        {
            return $"/api/entry/{teamId}/history/";
        }
    }
}
