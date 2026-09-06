using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Fpl.Client;

public class EventStatusClient(HttpClient client) : IEventStatusClient
{
    public async Task<EventStatusResponse?> GetEventStatus(CancellationToken ct)
    {
        return await client.GetFromJsonAsync<EventStatusResponse>($"/api/event-status/", cancellationToken: ct);
    }
}
