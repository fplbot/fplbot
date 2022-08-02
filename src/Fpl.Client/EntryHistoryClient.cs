using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System.Net;
using System.Net.Http.Json;

namespace Fpl.Client;

public class EntryHistoryClient : IEntryHistoryClient
{
    private readonly HttpClient _client;

    public EntryHistoryClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<(int teamId, EntryHistory entryHistory)?> GetHistory(int teamId, bool tolerate404 = false)
    {
        try
        {
            return (teamId, await _client.GetFromJsonAsync<EntryHistory>($"/api/entry/{teamId}/history/", JsonConvert.JsonSerializerOptions));
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
        {
            return null;
        }
    }
}
