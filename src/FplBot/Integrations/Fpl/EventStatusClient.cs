using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Fpl.Client;

public class EventStatusClient(HttpClient client) : IEventStatusClient
{
    public async Task<EventStatusResponse?> GetEventStatus()
    {
        return await client.GetFromJsonAsync<EventStatusResponse>($"/api/event-status/");
    }
}
