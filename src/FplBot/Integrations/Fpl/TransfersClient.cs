using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Fpl.Client;

public class TransfersClient(HttpClient client) : ITransfersClient
{
    public async Task<ICollection<Transfer>?> GetTransfers(int teamId)
    {
        return await client.GetFromJsonAsync<ICollection<Transfer>>($"/api/entry/{teamId}/transfers", JsonConvert.JsonSerializerOptions);
    }
}
