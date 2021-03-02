using System.Net.Http;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Newtonsoft.Json;

namespace Fpl.Client
{
    public class EventStatusClient : IEventStatusClient
    {
        private readonly HttpClient _client;

        public EventStatusClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<EventStatusResponse> GetEventStatus()
        {
            var json = await _client.GetStringAsync($"/api/event-status/");
            return JsonConvert.DeserializeObject<EventStatusResponse>(json);
        }
    }
}