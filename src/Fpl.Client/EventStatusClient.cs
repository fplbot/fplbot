using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;


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
            return await _client.GetFromJsonAsync<EventStatusResponse>($"/api/event-status/");
        }
    }
}
