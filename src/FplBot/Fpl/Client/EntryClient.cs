using Fpl.Client.Abstractions;
using Fpl.Client.Models;

using System.Net;
using System.Net.Http.Json;

namespace Fpl.Client;

public class EntryClient : IEntryClient
{
    private readonly HttpClient _client;

    public EntryClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<BasicEntry> Get(int teamId, bool tolerate404 = false)
    {
        try
        {
            return await _client.GetFromJsonAsync<BasicEntry>($"/api/entry/{teamId}/", JsonConvert.JsonSerializerOptions);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
        {
            return null;
        }
    }

    public async Task<EntryPicks> GetPicks(int teamId, int gameweek, bool tolerate404 = false)
    {
        try
        {
            return await _client.GetFromJsonAsync<EntryPicks>($"/api/entry/{teamId}/event/{gameweek}/picks/", JsonConvert.JsonSerializerOptions);
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound && tolerate404)
        {
            return null;
        }
    }
}
