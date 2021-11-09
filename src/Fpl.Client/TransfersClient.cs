using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System.Net.Http.Json;

namespace Fpl.Client;

public class TransfersClient : ITransfersClient
{
    private readonly HttpClient _client;

    public TransfersClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<ICollection<Transfer>> GetTransfers(int teamId)
    {
        return await _client.GetFromJsonAsync<ICollection<Transfer>>($"/api/entry/{teamId}/transfers", JsonConvert.JsonSerializerOptions);
    }
}