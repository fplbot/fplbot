using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fpl.Client
{
    public class TransfersClient : ITransfersClient
    {
        private readonly HttpClient _client;

        public TransfersClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ICollection<Transfer>> GetTransfers(int teamId)
        {
            var json = await _client.GetStringAsync($"/api/entry/{teamId}/transfers");

            return JsonConvert.DeserializeObject<ICollection<Transfer>>(json);
        }
    }
}
