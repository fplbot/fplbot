using Fpl.Client.Abstractions;
using Fpl.Client.Models;

using System.Net;

namespace Fpl.Client;

public class EntryClient(HttpClient client) : IEntryClient
{
    public async Task<BasicEntry?> Get(int teamId, bool tolerate404 = false)
    {
        try
        {
            return await client.GetFromJsonAsync<BasicEntry>($"/api/entry/{teamId}/", JsonConvert.JsonSerializerOptions);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
        {
            return null;
        }
    }

    public async Task<EntryPicks?> GetPicks(int teamId, int gameweek, bool tolerate404 = false)
    {
        try
        {
            return await client.GetFromJsonAsync<EntryPicks>($"/api/entry/{teamId}/event/{gameweek}/picks/", JsonConvert.JsonSerializerOptions);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
        {
            return null;
        }
    }
}
