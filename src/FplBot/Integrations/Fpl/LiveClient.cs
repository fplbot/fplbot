using Fpl.Client.Abstractions;
using Fpl.Client.Models;

namespace Fpl.Client;

public class LiveClient(HttpClient httpClient, ICacheProvider client) : ILiveClient
{
    public async Task<ICollection<LiveItem>?> GetLiveItems(int gameweek, bool isOngoingGameweek = false)
    {
        var response = await client.GetCachedOrFetch<LiveResponse>($"/api/event/{gameweek}/live/", httpClient.GetStringAsync, isOngoingGameweek ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(24));
        return response?.Elements;
    }
}
