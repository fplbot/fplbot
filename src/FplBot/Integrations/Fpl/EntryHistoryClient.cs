using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using System.Net;

namespace Fpl.Client;

public class EntryHistoryClient(HttpClient client) : IEntryHistoryClient
{
    public async Task<(int teamId, EntryHistory entryHistory)?> GetHistory(int teamId, bool tolerate404 = false)
    {
        try
        {
            return (teamId, await client.GetFromJsonAsync<EntryHistory>($"/api/entry/{teamId}/history/", JsonConvert.JsonSerializerOptions))!;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
        {
            return null;
        }
    }
}
